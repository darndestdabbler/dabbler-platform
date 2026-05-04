"""
Session 10 orchestrator for `security-dashboard-uat-remediation` (chunked).

Drives Steps 2, 3, 4, and 5 of the spec's Session 10 plan through the
AI router. The first attempt at this orchestrator shipped the full
Playwright coverage file (~1400 lines) into a single Opus
uat-plan-generation call and consistently hit the 300s httpx read
timeout. This revision chunks the work:

  Step 2 (`session-10/generate-structured-uat-plan`)
    For each FunctionalArea bucket, call
    `route(task_type="uat-plan-generation")` with ONLY the Playwright
    method bodies that belong to that bucket. Concatenate each
    returned Review[] slice into the final checklist.

  Step 3 (`session-10/uat-coverage-review-gate`)
    For each Review item in the generated checklist, call
    `route(task_type="uat-coverage-review")` with just that item and
    the one Playwright method body it references. The gate passes
    only when every per-item verdict is VERIFIED.

  Change-log generation (`session-10/generate-change-log`) is handled
  by a separate script after the final green run completes.

Intentionally scratch-quality: archived with the Session 10 commit.
"""

import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Optional

from ai_router import route

REPO = Path(__file__).resolve().parents[1]
SESSION_SET = REPO / "docs" / "session-sets" / "security-dashboard-uat-remediation"
PLAYWRIGHT_FILE = REPO / "tests" / "AcmeCoffeeOnline.Playwright" / "SecurityDashboardUatCoverageTests.cs"
SCHEMA_FILE = Path(r"c:\Users\denmi\source\repos\uat-checklist-editor\checklist-schema.json")
CHECKLIST_FILE = SESSION_SET / "security-dashboard-uat-remediation-uat-checklist.json"
SPEC_FILE = SESSION_SET / "spec.md"
REVIEWS_DIR = SESSION_SET / "session-reviews"


# FunctionalArea buckets. Each tuple is
#   (FunctionalArea, subarea-hint, [Playwright method names in this bucket]).
# Every one of the 34 methods in SecurityDashboardUatCoverageTests.cs
# belongs to exactly one bucket.
FA_BUCKETS: list[tuple[str, str, list[str]]] = [
    (
        "Shell and Access",
        "per-role Security nav visibility and dashboard tab gating (finding 2)",
        [
            "SecurityNav_SystemAdmin_NavVisible_And_FiveTabsAsync",
            "SecurityNav_CrossOrgUserAdmin_NavVisible_And_ThreeTabsAsync",
            "SecurityNav_BakedInUser_NavVisible_And_RoleDocOnlyAsync",
        ],
    ),
    (
        "Users and Role Assignment",
        "Users tab circuit stability (finding 1) + filter-change summary clear (findings 7/8)",
        [
            "UsersTab_LoadsForSystemAdmin_NoCircuitCrashAsync",
            "UsersTab_LoadsForSystemAdmin_GridProjectsSeededRowsAsync",
            "UsersTab_RapidRemountDoesNotCrashAsync",
            "UsersTab_LoadsForCrossOrgUserAdmin_NoCircuitCrashAsync",
            "UsersAndRoleAssignment_FilterChange_ClearsCollapsedSummaryStateAsync",
        ],
    ),
    (
        "Organization Master Details",
        "org-scope filter (finding 3), dash-in-filter (finding 3 sub), in-circuit refresh + New org cue (finding 6), shell summary reset on filter change (findings 7/8)",
        [
            "Customers_CrossOrgUserAdmin_SeesAllRetailCustomersAsync",
            "Customers_BackofficeUser_SeesCustomersInsteadOfEmptySetAsync",
            "OrgMasterDetails_FilterKeyContainsDash_ShowsRetailRowsInCircuitAsync",
            "OrgMasterDetails_CreateOrganization_GridRefreshesInCircuitAsync",
            "OrganizationMasterDetails_NewOrganization_CollapsesShellWithNewSummaryAsync",
            "OrganizationMasterDetails_FilterChange_ClearsCollapsedSummaryStateAsync",
        ],
    ),
    (
        "Data-Bound Restriction Enforcement",
        "restriction-seam reachability + ViewOnly/Hidden rendering across personas (finding 4)",
        [
            "Products_BackofficeUser_ReachesListWithoutAccessDeniedAsync",
            "Products_BackofficeUser_UnitPriceColumn_IsNotEditableAsync",
            "Products_BackofficeUser_UnitPriceField_IsDisabledOnDetailAsync",
            "AdminProducts_BackofficeUser_StillDeniedAsync",
            "Products_WarehouseUser_UnitPriceColumn_AbsentFromGridAsync",
            "Products_WarehouseUser_UnitPriceField_AbsentFromDetailAsync",
            "Products_CustomerPersona_UnitPriceField_IsDisabledOnDetailAsync",
        ],
    ),
    (
        "Default Masks and Column Restrictions",
        "safety-net removal (finding 5d), SSN seed visibility (finding 5a), Add Column Restriction enabled (finding 5b), singular entity names (finding 5c)",
        [
            "DefaultMasks_SafetyNetPanel_AbsentFromTabAsync",
            "DefaultMasks_SeededSsnRow_RendersSingularTableAndSsnColumnAsync",
            "DefaultMasks_TableDropdown_ShowsSingularEntityNamesAsync",
            "DefaultMasks_AddPhoneMask_PersistsAndRendersInGridAsync",
            "ColumnRestrictions_AddButton_EnabledForFreshPair_AndRowPersistsAsync",
            "ColumnRestrictions_AlreadyRestrictedColumn_HiddenFromDropdownAsync",
        ],
    ),
    (
        "Master-Details Expansion Shell",
        "row-select collapse + re-expand + filter-change reset + DisplayAlias width (findings 7, 8)",
        [
            "RoleMasterDetails_SelectRow_CollapsesShellAndShowsSummaryAsync",
            "RoleMasterDetails_SelectBakedInRow_CollapsesShellWithBakedInBadgeAsync",
            "RoleMasterDetails_ReexpandShellHeader_ShowsGridAgainAsync",
            "RoleMasterDetails_FilterChange_ClearsCollapsedSummaryStateAsync",
            "DefaultMasks_Shell_RendersDeclaredCountSummaryAsync",
            "RoleMasterGrid_DisplayAliasColumn_IsReadableAndNotClippedAsync",
        ],
    ),
    (
        "Accessibility and Keyboard",
        "tab-strip arrow-key handler removed; standard Tab focus still works (finding 9)",
        [
            "Dashboard_TabStrip_NoArrowKeyHandler_NormalTabFocusStillWorksAsync",
        ],
    ),
]


def extract_test_method_names(src: str) -> list[str]:
    return re.findall(r"public async Task\s+(\w+Async)\s*\(", src)


def extract_method_blocks(src: str) -> dict[str, str]:
    """Return {method_name: "comment block + [Fact] + method source"}.

    Brace-matches from the opening { of the method body to its close.
    Preceding `// ...` comment lines and the `[Fact]`/`[Theory]` line are
    included so the router call sees the UAT intent alongside the test
    body.
    """
    lines = src.splitlines(keepends=True)
    blocks: dict[str, str] = {}
    i = 0
    method_re = re.compile(
        r"^\s*public\s+async\s+Task\s+(\w+Async)\s*\("
    )
    while i < len(lines):
        m = method_re.match(lines[i])
        if not m:
            i += 1
            continue
        name = m.group(1)

        # Walk backward to pick up the [Fact]/[Theory] line and any
        # immediately-preceding // comment lines.
        start = i
        j = i - 1
        while j >= 0 and (
            lines[j].lstrip().startswith("//")
            or lines[j].lstrip().startswith("[Fact")
            or lines[j].lstrip().startswith("[Theory")
            or lines[j].lstrip().startswith("[InlineData")
        ):
            start = j
            j -= 1

        # Find opening {
        k = i
        while k < len(lines) and "{" not in lines[k]:
            k += 1
        if k == len(lines):
            i += 1
            continue

        depth = 0
        opened = False
        end = k
        while end < len(lines):
            for ch in lines[end]:
                if ch == "{":
                    depth += 1
                    opened = True
                elif ch == "}":
                    depth -= 1
                    if opened and depth == 0:
                        break
            if opened and depth == 0:
                break
            end += 1

        blocks[name] = "".join(lines[start:end + 1])
        i = end + 1

    return blocks


def method_ref(name: str) -> str:
    return f"SecurityDashboardUatCoverageTests.{name}"


def strip_json_fences(text: str) -> str:
    t = text.strip()
    if t.startswith("```"):
        t = t.split("\n", 1)[1] if "\n" in t else t
    if t.endswith("```"):
        t = t.rsplit("```", 1)[0]
    return t.strip()


def parse_json_tolerant(text: str):
    """
    Parse a JSON document that may be surrounded by code fences OR have
    leading prose / trailing commentary. Uses `JSONDecoder.raw_decode` so
    that "Here is the JSON:\n[ ... ]\nNote: ..." parses to the array.

    Raises `json.JSONDecodeError` with full context if no JSON document
    can be located.
    """
    stripped = strip_json_fences(text)
    decoder = json.JSONDecoder()
    # Scan forward to the first '[' or '{' — the model may emit a short
    # preamble before the JSON body even though the prompt forbids it.
    for i, ch in enumerate(stripped):
        if ch in "[{":
            try:
                obj, _end = decoder.raw_decode(stripped[i:])
                return obj
            except json.JSONDecodeError:
                continue
    # Last-ditch: raise the most informative error we can surface.
    return json.loads(stripped)


# ---------- Step 2: per-FA plan generation --------------------------------


def build_plan_prompt_for_bucket(
    fa: str,
    subarea_hint: str,
    method_bodies: list[tuple[str, str]],
) -> tuple[str, str]:
    """One router call per FA bucket. Return (content, context)."""
    bucket_method_refs = [method_ref(name) for name, _ in method_bodies]
    refs_enum = "\n".join(f"- {r}" for r in bucket_method_refs)

    content = rf"""
Produce the `Review[]` items for FunctionalArea = "{fa}" in the Session 10
UAT checklist for the `security-dashboard-uat-remediation` session set.

Subarea hint: {subarea_hint}

Output MUST be valid JSON — a single JSON array of Review items, no
outer object, no markdown fences, no prose before or after. Each item
conforms exactly to the extended UAT Checklist schema in <schema>.

## Per-item requirements

- `FunctionalArea`: "{fa}" (literal value for every item you produce).
- `Subarea`: your pick of a second-level group within {fa!r}.
- `ItemLabel`: specific, human-readable ("BackofficeUser reaches /products
  without an access-denied box").
- `Steps[]`: one step per semantic action the Playwright test performs.
  Each step carries:
    - `StepNumber` (integer, 1-based)
    - `Action` (string; use `{{{{VariableName}}}}` placeholders for parameters)
    - `Verifications[]` where each carries:
        - `Text` (required)
        - `VerificationMethod` in {{"ui","api","sql"}} (default "ui")
        - `Query` + `ExpectedValue` for sql/api where useful
- `TestData`: object mapping every `{{{{Variable}}}}` used in that item's
  Steps to a concrete value (persona email, URL, tab name, role name, etc.)
- `E2ETestReference`: MUST be one of these methods (one per item; every
  method in this list must be referenced by at least one item you emit):

{refs_enum}

- `IsJudgmentItem`: set `true` ONLY for items whose expectation is
  genuinely aesthetic ("DisplayAlias font matches sibling columns"). Still
  carry an `E2ETestReference` for sequence reachability plus a one-sentence
  `Feedback` justifying the flag. Cap judgment items at AT MOST 20% of
  the items you emit in this bucket.

## Step N = Step N discipline

Step N of each Review item MUST describe the same action the referenced
Playwright test performs at its Nth semantic step, with the same
parameters. Do NOT paraphrase. If the test does
`GotoAsync("/admin/security")` then clicks a tab named
"Role Master Details", Step 1 is `"Navigate to {{{{DashboardUrl}}}}"` with
TestData.DashboardUrl = "/admin/security" and Step 2 is
`"Click the {{{{TabName}}}} tab"` with TestData.TabName = "Role Master Details".

Each Review item covers EXACTLY one E2ETestReference. Do not merge
multiple methods into one item. Every method in the list above must
appear as exactly one item's E2ETestReference (1-to-1 with the bucket).

Return a JSON array only. No leading commentary, no trailing commentary.
""".strip()

    # Context: spec, schema, just the method bodies in this bucket.
    schema_text = SCHEMA_FILE.read_text(encoding="utf-8")
    spec_text = SPEC_FILE.read_text(encoding="utf-8")

    method_section = []
    for name, body in method_bodies:
        method_section.append(
            f"<test name=\"{method_ref(name)}\">\n{body}\n</test>"
        )
    methods_blob = "\n\n".join(method_section)

    context = (
        "<session-set-spec>\n"
        f"{spec_text}\n"
        "</session-set-spec>\n\n"
        "<schema>\n"
        f"{schema_text}\n"
        "</schema>\n\n"
        "<playwright-methods-in-this-bucket>\n"
        f"{methods_blob}\n"
        "</playwright-methods-in-this-bucket>\n"
    )

    return content, context


def generate_checklist(route, blocks: dict[str, str]) -> dict:
    """Run one router call per FA bucket, accumulate Review items."""
    all_review: list[dict] = []
    generator_records: list[dict] = []

    for fa, subarea_hint, names in FA_BUCKETS:
        missing = [n for n in names if n not in blocks]
        if missing:
            raise SystemExit(
                f"[session10] FA {fa!r} references missing methods: {missing}"
            )

        method_bodies = [(n, blocks[n]) for n in names]
        content, context = build_plan_prompt_for_bucket(
            fa, subarea_hint, method_bodies
        )

        print(
            f"[session10] Step 2: routing uat-plan-generation for "
            f"FA={fa!r} ({len(names)} methods) ...",
            flush=True,
        )
        result = route(
            content=content,
            task_type="uat-plan-generation",
            context=context,
            complexity_hint=70,
            max_tier=3,
            session_set=str(SESSION_SET),
            session_number=10,
        )
        print(
            f"  model={result.model_name} tier={result.tier} "
            f"cost=${result.total_cost_usd:.4f} "
            f"in={result.input_tokens} out={result.output_tokens}",
            flush=True,
        )

        try:
            items = parse_json_tolerant(result.content)
        except json.JSONDecodeError as exc:
            snippet = result.content[:2000]
            raise SystemExit(
                f"[session10] FA {fa!r} returned non-JSON:\n{exc}\n---\n{snippet}"
            )
        if not isinstance(items, list):
            raise SystemExit(
                f"[session10] FA {fa!r} did not return a JSON array; "
                f"got {type(items).__name__}"
            )
        for it in items:
            if not isinstance(it, dict):
                raise SystemExit(
                    f"[session10] FA {fa!r} produced a non-object item: {it!r}"
                )
            # Force FA value to the bucket's canonical name.
            it["FunctionalArea"] = fa
        all_review.extend(items)

        gen_record = {
            "functional_area": fa,
            "model": result.model_name,
            "cost_usd": round(result.total_cost_usd, 6),
            "input_tokens": result.input_tokens,
            "output_tokens": result.output_tokens,
            "items_emitted": len(items),
        }
        if result.verification is not None:
            gen_record["verifier_model"] = result.verification.verifier_model
            gen_record["verifier_verdict"] = result.verification.verdict
        generator_records.append(gen_record)

    checklist = {
        "ProjectName": "Dabbler Platform / AcmeCoffeeOnline Security Dashboard (UAT Remediation)",
        "ReleaseLabel": "security-dashboard-uat-remediation",
        "Review": all_review,
    }

    REVIEWS_DIR.mkdir(exist_ok=True)
    (REVIEWS_DIR / "session-010-plan-generation.json").write_text(
        json.dumps(generator_records, indent=2), encoding="utf-8"
    )

    return checklist


# ---------- Step 2 (post-generate) local validation ------------------------


def validate_checklist_schema(path: Path) -> tuple[bool, str]:
    result = subprocess.run(
        [
            str(REPO / ".venv" / "Scripts" / "python.exe"),
            "-m", "jsonschema",
            "-i", str(path),
            str(SCHEMA_FILE),
        ],
        capture_output=True, text=True,
    )
    return result.returncode == 0, (result.stderr or result.stdout)


def extra_local_checks(path: Path, valid_method_names: list[str]) -> list[str]:
    problems: list[str] = []
    doc = json.loads(path.read_text(encoding="utf-8"))
    review = doc.get("Review", [])
    if not review:
        problems.append("Review[] is empty.")
        return problems

    valid_set = set(valid_method_names)
    method_ref_pattern = re.compile(
        r"^SecurityDashboardUatCoverageTests\.(\w+Async)$"
    )
    placeholder_pattern = re.compile(r"\{\{(\w+)\}\}")

    referenced: set[str] = set()
    judgment_count = 0

    for idx, item in enumerate(review):
        label = item.get("ItemLabel", f"<unnamed item {idx}>")
        if bool(item.get("IsJudgmentItem", False)):
            judgment_count += 1

        ref = item.get("E2ETestReference")
        if ref is None:
            problems.append(f"[{label}] missing E2ETestReference.")
        else:
            m = method_ref_pattern.match(ref)
            if not m:
                problems.append(
                    f"[{label}] E2ETestReference {ref!r} does not match "
                    f"SecurityDashboardUatCoverageTests.<MethodName>Async pattern."
                )
            elif m.group(1) not in valid_set:
                problems.append(
                    f"[{label}] E2ETestReference {ref!r} does not resolve to "
                    f"a real method in SecurityDashboardUatCoverageTests.cs."
                )
            else:
                referenced.add(m.group(1))

        test_data = item.get("TestData") or {}
        for step in (item.get("Steps") or []):
            action = step.get("Action", "")
            for placeholder in placeholder_pattern.findall(action):
                if placeholder not in test_data:
                    problems.append(
                        f"[{label}] Steps[{step.get('StepNumber')}] "
                        f"references {{{{{placeholder}}}}} but TestData has "
                        f"no such key."
                    )

    unreferenced = valid_set - referenced
    if unreferenced:
        problems.append(
            "Playwright methods with no Review item: "
            + ", ".join(sorted(unreferenced))
        )

    total = len(review)
    if total > 0 and judgment_count * 5 > total:
        problems.append(
            f"IsJudgmentItem items {judgment_count}/{total} exceed the 20% "
            f"cap. Demote judgment items to mechanical verifications."
        )

    return problems


# ---------- Step 3: per-item coverage-review gate ---------------------------


def build_coverage_review_prompt(
    item: dict,
    method_name: str,
    method_body: str,
) -> tuple[str, str]:
    """One router call per Review item."""
    content = rf"""
Verify step-by-step parity between the single UAT Review item in
<review-item> and the single Playwright test method in <playwright-method>.

Gate rules:

(a) The item's `E2ETestReference` must name the playwright method in
    <playwright-method>.
(b) For each `Steps[N]` in the item: the Nth semantic action the
    Playwright test performs (ignoring boilerplate like `CreatePageAsync`,
    `try/finally`, `ClosePageAsync`) must match the item's
    `Steps[N].Action` after resolving `{{{{Variable}}}}` placeholders
    against `TestData`. "Match" means same action kind + same parameters
    (persona email, URL, tab name, entity names).
(c) For each `Steps[N].Verifications[]`: the test must perform an
    assertion that corresponds to each verification. UI verifications map
    to `Expect(...).ToBeVisibleAsync()` / `ToHaveCountAsync` /
    `ToBeDisabledAsync` / `ToBeEnabledAsync` / an `Assert.Equal` on
    innerText / etc. SQL/API verifications must describe the actual
    outcome the test proves end-to-end.

If the item has `IsJudgmentItem: true`, enforce only rule (a). For
judgment items, do not insist on per-step or per-verification parity —
the sequence-reachability is enough.

If (a), (b), (c) all hold (or just (a) for judgment items), respond with
exactly the single word:

    VERIFIED

Otherwise respond with:

    ISSUES_FOUND
    <one line per concrete mismatch>

Do not explain away mismatches. Be mechanical. Prefer false-positive
ISSUES_FOUND over false-negative VERIFIED.
""".strip()

    context = (
        "<review-item>\n"
        f"{json.dumps(item, indent=2)}\n"
        "</review-item>\n\n"
        f"<playwright-method name=\"{method_ref(method_name)}\">\n"
        f"{method_body}\n"
        "</playwright-method>\n"
    )
    return content, context


def run_coverage_gate(route, checklist_doc: dict, blocks: dict[str, str]) -> bool:
    """Return True iff every item yields VERIFIED."""
    review = checklist_doc.get("Review", [])
    verdicts: list[dict] = []
    all_ok = True
    method_ref_pattern = re.compile(
        r"^SecurityDashboardUatCoverageTests\.(\w+Async)$"
    )

    for idx, item in enumerate(review):
        label = item.get("ItemLabel", f"<item {idx}>")
        ref = item.get("E2ETestReference", "")
        m = method_ref_pattern.match(ref)
        if not m:
            verdicts.append({
                "index": idx, "label": label, "verdict": "ISSUES_FOUND",
                "detail": f"E2ETestReference {ref!r} is not well-formed.",
                "is_judgment": bool(item.get("IsJudgmentItem")),
            })
            all_ok = False
            continue
        method_name = m.group(1)
        body = blocks.get(method_name)
        if body is None:
            verdicts.append({
                "index": idx, "label": label, "verdict": "ISSUES_FOUND",
                "detail": f"Method {method_name} not found in coverage file.",
                "is_judgment": bool(item.get("IsJudgmentItem")),
            })
            all_ok = False
            continue

        content, context = build_coverage_review_prompt(item, method_name, body)
        print(
            f"[session10] Step 3: gate item {idx+1}/{len(review)} "
            f"({label!r}) -> {method_name}",
            flush=True,
        )
        result = route(
            content=content,
            task_type="uat-coverage-review",
            context=context,
            complexity_hint=60,
            max_tier=3,
            session_set=str(SESSION_SET),
            session_number=10,
        )
        body_text = result.content.strip()
        first = body_text.splitlines()[0].strip().upper() if body_text else ""
        verdict_ok = first.startswith("VERIFIED")

        verdicts.append({
            "index": idx,
            "label": label,
            "is_judgment": bool(item.get("IsJudgmentItem")),
            "method": method_name,
            "verdict": "VERIFIED" if verdict_ok else "ISSUES_FOUND",
            "model": result.model_name,
            "cost_usd": round(result.total_cost_usd, 6),
            "raw": body_text,
        })
        print(
            f"  {first or '<empty>'} "
            f"(model={result.model_name}, cost=${result.total_cost_usd:.4f})",
            flush=True,
        )
        if not verdict_ok:
            all_ok = False

    REVIEWS_DIR.mkdir(exist_ok=True)
    (REVIEWS_DIR / "session-010-coverage-review.json").write_text(
        json.dumps(verdicts, indent=2), encoding="utf-8"
    )

    return all_ok


# ---------- Entry ----------------------------------------------------------


def main():
    pw_text = PLAYWRIGHT_FILE.read_text(encoding="utf-8")
    method_names = extract_test_method_names(pw_text)
    print(f"[session10] Enumerated {len(method_names)} Playwright test methods.", flush=True)
    blocks = extract_method_blocks(pw_text)

    # Sanity: every bucket name must resolve; every method must belong to exactly one bucket.
    bucket_coverage: set[str] = set()
    for _, _, names in FA_BUCKETS:
        for n in names:
            if n in bucket_coverage:
                raise SystemExit(f"[session10] method {n} appears in two buckets.")
            bucket_coverage.add(n)
    missing_from_buckets = set(method_names) - bucket_coverage
    if missing_from_buckets:
        raise SystemExit(
            "[session10] methods not assigned to any bucket: "
            + ", ".join(sorted(missing_from_buckets))
        )
    extraneous = bucket_coverage - set(method_names)
    if extraneous:
        raise SystemExit(
            "[session10] bucket entries that don't exist as methods: "
            + ", ".join(sorted(extraneous))
        )

    # ---- Step 2: per-FA plan generation ---------------------------------
    checklist = generate_checklist(route, blocks)
    CHECKLIST_FILE.write_text(
        json.dumps(checklist, indent=2) + "\n", encoding="utf-8"
    )
    print(
        f"[session10] Wrote {CHECKLIST_FILE.relative_to(REPO)} "
        f"({len(checklist['Review'])} Review items)",
        flush=True,
    )

    # ---- Step 2 post-generate validation --------------------------------
    print("[session10] Step 2 validation: schema + placeholders + refs + judgment cap ...", flush=True)
    ok, out = validate_checklist_schema(CHECKLIST_FILE)
    if not ok:
        raise SystemExit(f"[session10] jsonschema validation failed:\n{out}")
    print("  schema: OK", flush=True)

    problems = extra_local_checks(CHECKLIST_FILE, method_names)
    if problems:
        detail = "\n".join(f"    - {p}" for p in problems)
        raise SystemExit(f"[session10] local checks failed:\n{detail}")
    print("  placeholders / refs / judgment-cap: OK", flush=True)

    # ---- Step 3: per-item uat-coverage-review gate ----------------------
    all_ok = run_coverage_gate(route, checklist, blocks)
    if not all_ok:
        raise SystemExit(
            "[session10] uat-coverage-review gate FAILED — see "
            "session-reviews/session-010-coverage-review.json for per-item verdicts."
        )
    print("[session10] uat-coverage-review: VERIFIED for every item.", flush=True)


if __name__ == "__main__":
    main()
