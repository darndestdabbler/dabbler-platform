"""UAT runner: compile a checklist via W0, run via `dotnet test`, parse .trx.

Public entry point is :func:`run_checklist`. Triage is wired in Session 2
via :class:`uat_runner.triage.TriageSession`; remediation dispatch
lands in Session 3.

End-to-end pipeline:

1. Invoke ``dabbler-uat-dsl compile`` (W0 CLI) on the checklist with
   the catalog. The CLI emits a static Playwright C# helper class to
   ``tests/AcmeCoffeeOnline.Playwright/Generated/<ClassName>.cs``.
2. Write a sibling ``<ClassName>Tests.cs`` xUnit wrapper that calls
   ``<ClassName>.RunAsync(adapter, page)`` from a single ``[Fact]``.
3. Invoke ``dotnet test --filter`` against the wrapper class and emit
   a ``.trx`` logger output.
4. Parse the ``.trx`` to collect pass/fail outcomes and produce a
   :class:`RunReport`.
5. If failures exist, run each through :class:`TriageSession` (up to
   ``MAX_TRIAGE_CALLS`` calls / ``MAX_TRIAGE_COST_USD``) and attach
   ``TriageResult`` objects to the report.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable

REPO_ROOT = Path(__file__).resolve().parents[1]
GENERATED_DIR_RELATIVE = Path("tests/AcmeCoffeeOnline.Playwright/Generated")
PLAYWRIGHT_CSPROJ = (
    REPO_ROOT / "tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj"
)
W0_CLI_PROJECT = REPO_ROOT.parent / "dabbler-uat-dsl/src/Dabbler.Uat.Dsl.Cli"


@dataclass
class StepFailure:
    """A single failed test outcome lifted from a `.trx` UnitTestResult."""

    test_name: str
    outcome: str
    error_message: str
    error_stack_trace: str
    duration: str | None = None


@dataclass
class RunReport:
    checklist_path: str
    catalog_path: str
    class_name: str
    emitted_path: str
    wrapper_path: str
    compile_ok: bool
    compile_output: str
    test_invoked: bool
    test_exit_code: int | None
    trx_path: str | None
    passed: list[str] = field(default_factory=list)
    failed: list[StepFailure] = field(default_factory=list)
    runner_message: str = ""
    triage_results: list[Any] = field(default_factory=list)  # list[TriageResult]
    triage_total_cost_usd: float = 0.0

    @property
    def has_failures(self) -> bool:
        return (
            bool(self.failed)
            or not self.compile_ok
            or (self.test_invoked and self.test_exit_code != 0)
        )

    def to_dict(self) -> dict:
        return {
            "checklistPath": self.checklist_path,
            "catalogPath": self.catalog_path,
            "className": self.class_name,
            "emittedPath": self.emitted_path,
            "wrapperPath": self.wrapper_path,
            "compileOk": self.compile_ok,
            "compileOutput": self.compile_output,
            "testInvoked": self.test_invoked,
            "testExitCode": self.test_exit_code,
            "trxPath": self.trx_path,
            "passed": self.passed,
            "failed": [
                {
                    "testName": f.test_name,
                    "outcome": f.outcome,
                    "errorMessage": f.error_message,
                    "errorStackTrace": f.error_stack_trace,
                    "duration": f.duration,
                }
                for f in self.failed
            ],
            "triageResults": [
                {
                    "stepNumber": tr.failure.step_number,
                    "stepText": tr.failure.step_text,
                    "classification": tr.classification,
                    "confidence": tr.confidence,
                    "evidence": tr.evidence,
                    "proposedFix": tr.proposed_fix,
                    "triageCostUsd": tr.triage_cost_usd,
                    "skippedReason": tr.skipped_reason,
                }
                for tr in self.triage_results
            ],
            "triageTotalCostUsd": self.triage_total_cost_usd,
            "runnerMessage": self.runner_message,
        }


def _slug_to_class_name(slug: str) -> str:
    parts = re.split(r"[^A-Za-z0-9]+", slug)
    pascal = "".join(p[:1].upper() + p[1:] for p in parts if p)
    if not pascal:
        pascal = "UatChecklist"
    if not pascal[0].isalpha():
        pascal = "Uat" + pascal
    return pascal + "Uat"


def _generated_dir() -> Path:
    d = REPO_ROOT / GENERATED_DIR_RELATIVE
    d.mkdir(parents=True, exist_ok=True)
    return d


def _resolve_active_role(
    checklist_path: Path,
    explicit_role: str | None,
) -> tuple[str | None, str]:
    """Decide the active role to pin during W0 compile.

    Returns ``(role, message)``:

    - ``role`` is the value to pass to ``--active-role``, or ``None`` to
      let the validator default to the AnyRole wildcard.
    - ``message`` is a human-readable note appended to the runner output;
      it is non-empty when persona inference matched (informational) or
      when the checklist's email is missing from the persona map (fatal,
      which is signalled to the caller by ``role`` being ``None`` while
      ``message`` describes the failure).

    An explicit ``explicit_role`` skips inference entirely.
    """
    if explicit_role:
        return explicit_role, f"Active role pinned via override: {explicit_role}"

    from . import personas as _personas  # local import keeps the module side-effect free

    result = _personas.infer_active_role(checklist_path)
    if result.role is not None:
        return result.role, (
            f"Active role inferred from sign-in step ({result.email} -> {result.role})."
        )
    if result.error is not None:
        return None, f"Persona inference error: {result.error}"
    return None, ""


def _invoke_w0_compile(
    checklist_path: Path,
    catalog_path: Path,
    output_path: Path,
    active_role: str | None = None,
    suppressions_path: Path | None = None,
) -> tuple[bool, str]:
    cmd = [
        "dotnet",
        "run",
        "--project",
        str(W0_CLI_PROJECT),
        "--",
        "compile",
        str(checklist_path),
        "--catalog",
        str(catalog_path),
        "--emitter",
        "playwright-csharp",
        "--output",
        str(output_path),
    ]
    if active_role:
        cmd.extend(["--active-role", active_role])
    if suppressions_path and suppressions_path.is_file():
        cmd.extend(["--suppressions", str(suppressions_path)])
    proc = subprocess.run(
        cmd,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        timeout=600,
    )
    return proc.returncode == 0, (proc.stdout or "") + (proc.stderr or "")


_TEST_WRAPPER_TEMPLATE = """// <auto-generated />
// Generated by ai-router/uat_runner. Do not edit by hand.
using System.Threading.Tasks;
using AcmeCoffeeOnline.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace AcmeCoffeeOnline.Playwright.Generated;

[Collection(PlaywrightCollection.Name)]
public sealed class {class_name}Tests
{{
    private readonly PlaywrightFixture _fixture;

    public {class_name}Tests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RunChecklistAsync()
    {{
        var page = await _fixture.CreatePageAsync();
{seeded_signin_block}        var adapter = new PlaywrightFixtureUatAdapter(_fixture);
        await {class_name}.RunAsync(adapter, page);
    }}
}}
"""


def _write_test_wrapper(class_name: str, auth_email: str = "") -> Path:
    # The DSL has no primitives for the magic-link OTP-code paste flow used by
    # the Web app's real /login page. When the caller wires an `auth_email`,
    # seed the persona via the established test backdoor so the checklist's
    # actions start from a signed-in state — sign-in is test setup, the SUT
    # is the post-auth route the checklist exercises.
    seeded_signin_block = ""
    if auth_email:
        seeded_signin_block = (
            f'        await _fixture.SeededSignInAsync(page, "{auth_email}");\n'
        )
    wrapper_path = _generated_dir() / f"{class_name}Tests.cs"
    wrapper_path.write_text(
        _TEST_WRAPPER_TEMPLATE.format(
            class_name=class_name,
            seeded_signin_block=seeded_signin_block,
        ),
        encoding="utf-8",
    )
    return wrapper_path


def _invoke_dotnet_test(class_name: str, trx_path: Path) -> tuple[int, str]:
    cmd = [
        "dotnet",
        "test",
        str(PLAYWRIGHT_CSPROJ),
        "--filter",
        f"FullyQualifiedName~AcmeCoffeeOnline.Playwright.Generated.{class_name}Tests",
        "--logger",
        f"trx;LogFileName={trx_path.name}",
        "--results-directory",
        str(trx_path.parent),
        "--nologo",
    ]
    proc = subprocess.run(
        cmd,
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        timeout=900,
    )
    return proc.returncode, (proc.stdout or "") + (proc.stderr or "")


_TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def _parse_trx(trx_path: Path) -> tuple[list[str], list[StepFailure]]:
    """Return (passed_test_names, failed_steps) parsed from a trx file."""
    if not trx_path.is_file():
        return [], []

    tree = ET.parse(trx_path)
    root = tree.getroot()
    passed: list[str] = []
    failed: list[StepFailure] = []
    for result in root.iter(f"{_TRX_NS}UnitTestResult"):
        outcome = result.attrib.get("outcome", "")
        name = result.attrib.get("testName", "")
        duration = result.attrib.get("duration")
        if outcome == "Passed":
            passed.append(name)
            continue
        msg_node = result.find(f".//{_TRX_NS}Message")
        stack_node = result.find(f".//{_TRX_NS}StackTrace")
        failed.append(
            StepFailure(
                test_name=name,
                outcome=outcome,
                error_message=(msg_node.text or "").strip() if msg_node is not None else "",
                error_stack_trace=(stack_node.text or "").strip() if stack_node is not None else "",
                duration=duration,
            )
        )
    return passed, failed


def run_checklist(
    checklist_path: str | os.PathLike[str],
    catalog_path: str | os.PathLike[str],
    session_set_slug: str,
    base_url: str = "http://localhost:5010",
    auth_email: str = "admin@acmecoffee.com",
    skip_dotnet_test: bool = False,
    skip_triage: bool = False,
    session_set: str = "",
    session_number: int | None = None,
    active_role: str | None = None,
) -> RunReport:
    """Compile, run, and parse a UAT checklist.

    ``base_url`` and ``auth_email`` flow into the failure record sent to
    triage. Pass ``skip_dotnet_test=True`` to stop after compile.
    Pass ``skip_triage=True`` to skip triage even when failures exist (useful
    when testing the plumbing without incurring AI costs).
    ``session_set`` / ``session_number`` are threaded through to the triage
    router calls for metrics grouping.

    ``active_role`` overrides the role passed to the W0 CLI's
    ``--active-role`` flag. When ``None``, the runner infers the role
    from the checklist's first sign-in step via :mod:`personas`; if the
    persona map has no entry for the inferred email the run fails before
    compile rather than silently falling back to the AnyRole wildcard.
    """
    checklist = Path(checklist_path).resolve()
    catalog = Path(catalog_path).resolve()
    if not checklist.is_file():
        raise FileNotFoundError(f"Checklist not found: {checklist}")
    if not catalog.is_file():
        raise FileNotFoundError(f"Catalog not found: {catalog}")
    if shutil.which("dotnet") is None:
        raise RuntimeError("dotnet CLI not on PATH")
    if not (W0_CLI_PROJECT / "Dabbler.Uat.Dsl.Cli.csproj").is_file():
        raise RuntimeError(
            f"W0 CLI project not found at {W0_CLI_PROJECT}. Build dabbler-uat-dsl first."
        )

    class_name = _slug_to_class_name(session_set_slug)
    emitted_path = _generated_dir() / f"{class_name}.cs"

    resolved_role, role_message = _resolve_active_role(checklist, active_role)
    if role_message and not resolved_role:
        return RunReport(
            checklist_path=str(checklist),
            catalog_path=str(catalog),
            class_name=class_name,
            emitted_path=str(emitted_path),
            wrapper_path="",
            compile_ok=False,
            compile_output="",
            test_invoked=False,
            test_exit_code=None,
            trx_path=None,
            runner_message=role_message,
        )

    suppressions_path = REPO_ROOT / "tools/uat-suppressions.json"
    compile_ok, compile_output = _invoke_w0_compile(
        checklist,
        catalog,
        emitted_path,
        active_role=resolved_role,
        suppressions_path=suppressions_path,
    )
    if role_message:
        compile_output = f"{role_message}\n{compile_output}"

    report = RunReport(
        checklist_path=str(checklist),
        catalog_path=str(catalog),
        class_name=class_name,
        emitted_path=str(emitted_path),
        wrapper_path="",
        compile_ok=compile_ok,
        compile_output=compile_output,
        test_invoked=False,
        test_exit_code=None,
        trx_path=None,
    )
    if not compile_ok:
        # W0 1.2.0 honors --suppressions natively: matching errors are
        # downgraded to warnings server-side and the .cs is emitted when
        # all remaining errors are zero. Reaching this branch means W0
        # itself returned a non-zero exit, which implies one or more
        # unsuppressed real errors. Report directly without re-matching.
        report.runner_message = (
            "Compile failed — checklist did not pass W0 validation "
            "(W0 ran with --suppressions tools/uat-suppressions.json; "
            "any remaining errors are unsuppressed and real)."
        )
        return report

    wrapper_path = _write_test_wrapper(class_name, auth_email=auth_email)
    report.wrapper_path = str(wrapper_path)

    if skip_dotnet_test:
        report.runner_message = "Compile succeeded; dotnet test skipped (skip_dotnet_test=True)."
        return report

    trx_path = _generated_dir() / f"{class_name}.trx"
    if trx_path.exists():
        trx_path.unlink()

    exit_code, test_output = _invoke_dotnet_test(class_name, trx_path)
    report.test_invoked = True
    report.test_exit_code = exit_code
    report.trx_path = str(trx_path)

    passed, failed = _parse_trx(trx_path)
    report.passed = passed
    report.failed = failed

    if failed and not skip_triage:
        _run_triage(report, session_set=session_set, session_number=session_number)

    if failed:
        if report.triage_results:
            classified = ", ".join(
                f"{tr.classification}(step={tr.failure.step_number})"
                for tr in report.triage_results
            )
            report.runner_message = (
                f"{len(failed)} failure(s): {classified}. "
                f"Triage cost: ${report.triage_total_cost_usd:.4f}."
            )
        else:
            report.runner_message = (
                f"{len(failed)} test failure(s) reported via .trx."
            )
    elif passed:
        report.runner_message = f"All {len(passed)} test(s) passed."
    else:
        report.runner_message = (
            f"dotnet test exited {exit_code} but no UnitTestResult entries "
            "were found in the .trx — see test output."
        )
    return report


def _run_triage(
    report: "RunReport",
    session_set: str = "",
    session_number: int | None = None,
) -> None:
    """Run each StepFailure through TriageSession, apply remediations, and log outcomes."""
    from datetime import datetime
    from .triage import FailureRecord, TriageSession, apply_remediation

    ts = TriageSession(
        session_set=session_set or "docs/session-sets/uat-runner-and-triage-feedback",
        session_number=session_number,
    )

    # Extract session-set slug from the full session_set path
    session_set_slug = Path(session_set or "docs/session-sets/uat-runner-and-triage-feedback").name

    activity_log_path = (
        REPO_ROOT / "docs/session-sets" / session_set_slug / "activity-log.json"
    )

    for sf in report.failed:
        # Build a FailureRecord from StepFailure. Step number is inferred
        # from the test name (which encodes the step via the xUnit wrapper).
        # For Session 2, step_number is a best-effort parse; Session 3 will
        # carry explicit step metadata through the .trx.
        fr = FailureRecord(
            step_number=_parse_step_number(sf.test_name),
            step_text=sf.test_name,
            playwright_error=sf.error_message,
            current_url="",
        )
        result = ts.classify(fr)
        report.triage_results.append(result)

        # Apply the appropriate remediation based on classification
        remediation_outcome = apply_remediation(result, session_set_slug)

        # Log the remediation outcome to activity-log.json
        _append_activity_log(activity_log_path, remediation_outcome, session_number)

    report.triage_total_cost_usd = ts.total_cost_usd


def _append_activity_log(
    activity_log_path: Path,
    remediation_outcome: dict,
    session_number: int | None = None,
) -> None:
    """Append a remediation outcome record to the session-set activity-log.json.

    Creates the log if it doesn't exist. The log structure is defined in
    the uat-runner-and-triage-feedback spec.
    """
    from datetime import datetime

    if not activity_log_path.parent.exists():
        activity_log_path.parent.mkdir(parents=True, exist_ok=True)

    # Load existing log or create a new one
    if activity_log_path.exists():
        with open(activity_log_path, "r", encoding="utf-8") as f:
            log = json.load(f)
    else:
        log = {
            "sessionSetName": activity_log_path.parent.name,
            "createdDate": datetime.utcnow().isoformat() + "+00:00",
            "totalSessions": 4,
            "entries": [],
        }

    # Build the activity entry
    entry = {
        "sessionNumber": session_number or 3,
        "type": remediation_outcome.get("type", "uat-runner-failure"),
        "stepNumber": remediation_outcome.get("stepNumber", 0),
        "classification": remediation_outcome.get("classification", "UNKNOWN"),
        "autoApplied": remediation_outcome.get("autoApplied", False),
        "outcome": remediation_outcome.get("outcome", "unknown"),
        "dateTime": datetime.utcnow().isoformat() + "+00:00",
    }

    if "issueFile" in remediation_outcome:
        entry["issueFile"] = remediation_outcome["issueFile"]

    if "reason" in remediation_outcome:
        entry["reason"] = remediation_outcome["reason"]

    log["entries"].append(entry)

    # Write back
    with open(activity_log_path, "w", encoding="utf-8") as f:
        json.dump(log, f, indent=2, ensure_ascii=False)


def _split_compile_errors_by_suppressions(
    compile_output: str,
    checklist_path: str,
) -> tuple[list[dict], list[dict]]:
    """Partition W0 compile errors into (suppressed, unsuppressed) lists.

    Reads ``tools/uat-suppressions.json`` and matches each error from
    ``compile_output`` (which is the W0 CLI's JSON output on failure)
    against the suppression entries. An error is suppressed when **all**
    of these are true:

        - the suppression entry's ``checklists`` list contains the current
          checklist path (or matches it via fnmatch glob);
        - ``errorKind`` matches (or is ``"*"``);
        - the error's route path matches one of ``routePathPatterns``
          (matched by fnmatch glob; ``"*"`` matches all routes).

    Returns ``([], all_errors)`` if the suppressions file is missing or
    malformed — i.e., absence of suppressions means nothing is suppressed.
    """
    import fnmatch
    import re

    suppressions_path = REPO_ROOT / "tools/uat-suppressions.json"
    if not suppressions_path.is_file():
        return [], _parse_compile_errors(compile_output)

    try:
        suppressions_doc = json.loads(suppressions_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return [], _parse_compile_errors(compile_output)

    entries = suppressions_doc.get("suppressions", [])
    errors = _parse_compile_errors(compile_output)
    if not errors:
        return [], []

    # Normalise the checklist path for matching (forward slashes, relative
    # to repo root if possible).
    try:
        rel_checklist = str(
            Path(checklist_path).resolve().relative_to(REPO_ROOT)
        ).replace("\\", "/")
    except ValueError:
        rel_checklist = checklist_path.replace("\\", "/")

    suppressed: list[dict] = []
    unsuppressed: list[dict] = []
    for err in errors:
        kind = err.get("kind", "")
        message = err.get("message", "")
        # Pull the quoted route path out of the message body, if any.
        route_match = re.search(r'route [""]([^""]+)[""]', message)
        route_path = route_match.group(1) if route_match else ""

        matched = False
        for entry in entries:
            checklist_globs = entry.get("checklists", []) or []
            if not any(
                fnmatch.fnmatch(rel_checklist, glob) for glob in checklist_globs
            ):
                continue
            entry_kind = entry.get("errorKind", "")
            if entry_kind not in ("*", kind):
                continue
            patterns = entry.get("routePathPatterns", []) or []
            if route_path:
                if not any(fnmatch.fnmatch(route_path, p) for p in patterns):
                    continue
            else:
                # No route in the message — accept only if pattern is "*"
                if "*" not in patterns:
                    continue
            matched = True
            break

        if matched:
            suppressed.append(err)
        else:
            unsuppressed.append(err)

    return suppressed, unsuppressed


def _parse_compile_errors(compile_output: str) -> list[dict]:
    """Extract the ``errors`` array from a W0 CLI compile-failure JSON
    dump. Returns ``[]`` if the output is not parseable.
    """
    if not compile_output:
        return []
    # The CLI may emit additional stderr / progress lines around the JSON.
    # Heuristic: find the first '{' through the matching closing brace.
    start = compile_output.find("{")
    if start < 0:
        return []
    depth = 0
    end = -1
    for i in range(start, len(compile_output)):
        ch = compile_output[i]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                end = i
                break
    if end < 0:
        return []
    try:
        doc = json.loads(compile_output[start : end + 1])
    except json.JSONDecodeError:
        return []
    errors = doc.get("errors", []) if isinstance(doc, dict) else []
    return [e for e in errors if isinstance(e, dict)]


def _parse_step_number(test_name: str) -> int:
    """Extract a step number from a generated test name, or return 0."""
    m = re.search(r"Step(\d+)|step_(\d+)|_(\d+)$", test_name, re.IGNORECASE)
    if m:
        return int(next(g for g in m.groups() if g is not None))
    return 0


def _format_report_for_console(report: RunReport) -> str:
    lines = [
        f"Checklist: {report.checklist_path}",
        f"Catalog:   {report.catalog_path}",
        f"Class:     {report.class_name}",
        f"Emitted:   {report.emitted_path}",
        f"Wrapper:   {report.wrapper_path or '(none — compile failed)'}",
        f"Compile:   {'OK' if report.compile_ok else 'FAILED'}",
    ]
    if report.test_invoked:
        lines.append(f"dotnet test exit: {report.test_exit_code}")
        lines.append(f"trx:              {report.trx_path}")
        lines.append(f"Passed: {len(report.passed)}   Failed: {len(report.failed)}")
        for failure in report.failed:
            lines.append(f"  FAIL {failure.test_name}: {failure.error_message[:200]}")
    if report.runner_message:
        lines.append(f"Runner: {report.runner_message}")
    return "\n".join(lines)


def main(argv: Iterable[str] | None = None) -> int:
    import argparse

    parser = argparse.ArgumentParser(prog="uat_runner.runner")
    parser.add_argument("--checklist", required=True)
    parser.add_argument("--catalog", required=True)
    parser.add_argument("--session-set", required=True, dest="session_set")
    parser.add_argument("--base-url", default="http://localhost:5010")
    parser.add_argument("--auth-email", default="admin@acmecoffee.com")
    parser.add_argument("--skip-dotnet-test", action="store_true")
    parser.add_argument("--skip-triage", action="store_true",
                        help="Skip triage even when failures exist.")
    parser.add_argument("--session-number", type=int, default=None,
                        dest="session_number",
                        help="Session number for metrics grouping.")
    parser.add_argument("--json", action="store_true", help="Print RunReport as JSON.")
    args = parser.parse_args(list(argv) if argv is not None else None)

    report = run_checklist(
        checklist_path=args.checklist,
        catalog_path=args.catalog,
        session_set_slug=args.session_set,
        base_url=args.base_url,
        auth_email=args.auth_email,
        skip_dotnet_test=args.skip_dotnet_test,
        skip_triage=args.skip_triage,
        session_set=args.session_set,
        session_number=args.session_number,
    )

    if args.json:
        print(json.dumps(report.to_dict(), indent=2))
    else:
        print(_format_report_for_console(report))

    return 0 if not report.has_failures else 1


if __name__ == "__main__":
    sys.exit(main())
