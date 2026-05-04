"""
Session 10 per-item coverage-review gate — Sonnet + parallel.

Reads the checklist already written by session10_orchestrator.py and runs
the uat-coverage-review gate one call per Review item, explicitly against
Sonnet (`query(model="sonnet")` bypasses the router's hard pin on Opus
for this task type), with a bounded thread pool so the 34 sequential
calls become ~1 minute wall clock instead of ~15.

This is the "don't pay Anthropic to re-summon the orchestrator" pattern
applied narrowly: for uat-plan-generation, Opus-as-orchestrator already
authored the items, so no routing; for the gate, we specifically WANT a
different context than the author — Sonnet fits that fine and is much
cheaper + faster than calling into Opus again.

Scratch-quality; archived with the Session 10 commit.
"""

import json
import os
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

from ai_router import query

REPO = Path(__file__).resolve().parents[1]
SESSION_SET = REPO / "docs" / "session-sets" / "security-dashboard-uat-remediation"
PLAYWRIGHT_FILE = REPO / "tests" / "AcmeCoffeeOnline.Playwright" / "SecurityDashboardUatCoverageTests.cs"
SCHEMA_FILE = Path(r"c:\Users\denmi\source\repos\uat-checklist-editor\checklist-schema.json")
CHECKLIST_FILE = SESSION_SET / "security-dashboard-uat-remediation-uat-checklist.json"
REVIEWS_DIR = SESSION_SET / "session-reviews"

# Match the FA-bucket / method-extraction helpers in session10_orchestrator.py
# without importing that module (it auto-runs main()).


def extract_method_blocks(src: str) -> dict[str, str]:
    lines = src.splitlines(keepends=True)
    blocks: dict[str, str] = {}
    i = 0
    method_re = re.compile(r"^\s*public\s+async\s+Task\s+(\w+Async)\s*\(")
    while i < len(lines):
        m = method_re.match(lines[i])
        if not m:
            i += 1
            continue
        name = m.group(1)
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


def build_gate_prompt(item: dict, method_name: str, method_body: str) -> tuple[str, str]:
    content = rf"""
Verify step-by-step parity between the single UAT Review item in
<review-item> and the single Playwright test method in <playwright-method>.

Gate rules:

(a) The item's `E2ETestReference` must name the playwright method in
    <playwright-method>.
(b) For each `Steps[N]` in the item: the Nth semantic action the
    Playwright test performs (ignoring boilerplate like `CreatePageAsync`,
    `try/finally`, `ClosePageAsync`, `SeededSignInAsync` when it is the
    first setup call) must match the item's `Steps[N].Action` after
    resolving `{{{{Variable}}}}` placeholders against `TestData`. "Match"
    means same action kind + same parameters (persona email, URL,
    tab name, entity names).
(c) For each `Steps[N].Verifications[]`: the test must perform an
    assertion that corresponds to each verification. UI verifications map
    to `Expect(...).ToBeVisibleAsync()` / `ToHaveCountAsync` /
    `ToBeDisabledAsync` / `ToBeEnabledAsync` / `Assert.Equal` on innerText
    / etc. SQL/API verifications must describe an outcome the test
    proves end-to-end.

If the item has `IsJudgmentItem: true`, enforce only rule (a). For
judgment items do not insist on per-step or per-verification parity.

Respond with EXACTLY one of these two forms and nothing else:

    VERIFIED

or

    ISSUES_FOUND
    - <one bullet per concrete mismatch>

Prefer false-positive ISSUES_FOUND over false-negative VERIFIED. Be
mechanical; do not explain away mismatches.
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


def verify_one(query, item: dict, idx: int, total: int, blocks: dict[str, str]) -> dict:
    label = item.get("ItemLabel", f"<item {idx}>")
    ref = item.get("E2ETestReference", "")
    m = re.match(r"^SecurityDashboardUatCoverageTests\.(\w+Async)$", ref)
    if not m:
        return {
            "index": idx, "label": label, "verdict": "ISSUES_FOUND",
            "detail": f"E2ETestReference {ref!r} is not well-formed.",
            "is_judgment": bool(item.get("IsJudgmentItem")),
        }
    method_name = m.group(1)
    body = blocks.get(method_name)
    if body is None:
        return {
            "index": idx, "label": label, "verdict": "ISSUES_FOUND",
            "detail": f"Method {method_name} not found in coverage file.",
            "is_judgment": bool(item.get("IsJudgmentItem")),
        }

    content, context = build_gate_prompt(item, method_name, body)
    try:
        result = query(
            model="sonnet",
            content=content,
            task_type="uat-coverage-review",
            context=context,
            session_set=str(SESSION_SET),
            session_number=10,
        )
    except Exception as exc:
        return {
            "index": idx, "label": label, "verdict": "ISSUES_FOUND",
            "detail": f"API call raised: {exc}",
            "is_judgment": bool(item.get("IsJudgmentItem")),
        }

    body_text = (result.content or "").strip()
    first = body_text.splitlines()[0].strip().upper() if body_text else ""
    verdict_ok = first.startswith("VERIFIED")

    record = {
        "index": idx,
        "label": label,
        "is_judgment": bool(item.get("IsJudgmentItem")),
        "method": method_name,
        "verdict": "VERIFIED" if verdict_ok else "ISSUES_FOUND",
        "model": result.model_name,
        "cost_usd": round(result.total_cost_usd, 6),
        "raw": body_text,
    }
    status = "VERIFIED" if verdict_ok else "ISSUES_FOUND"
    print(
        f"  [{idx+1:>2}/{total}] {status:<13} {label}",
        flush=True,
    )
    return record


def main():
    if not CHECKLIST_FILE.exists():
        raise SystemExit(
            f"[session10-gate] checklist not found at {CHECKLIST_FILE}. "
            f"Run session10_orchestrator.py first (or author the checklist by hand)."
        )

    checklist = json.loads(CHECKLIST_FILE.read_text(encoding="utf-8"))
    review = checklist.get("Review", [])
    if not review:
        raise SystemExit("[session10-gate] Review[] is empty.")
    total = len(review)
    print(f"[session10-gate] {total} items to verify.", flush=True)

    pw_text = PLAYWRIGHT_FILE.read_text(encoding="utf-8")
    blocks = extract_method_blocks(pw_text)

    verdicts: list[dict] = [None] * total  # type: ignore[list-item]
    # 8-way parallel. Anthropic rate limit on sonnet is 50 rpm in the
    # config, so 8 concurrent for ~2s/call is well inside the budget.
    max_workers = 8
    print(
        f"[session10-gate] starting gate, sonnet, {max_workers}-way parallel ...",
        flush=True,
    )
    with ThreadPoolExecutor(max_workers=max_workers) as pool:
        futures = {
            pool.submit(verify_one, query, item, idx, total, blocks): idx
            for idx, item in enumerate(review)
        }
        for fut in as_completed(futures):
            idx = futures[fut]
            verdicts[idx] = fut.result()

    REVIEWS_DIR.mkdir(exist_ok=True)
    (REVIEWS_DIR / "session-010-coverage-review.json").write_text(
        json.dumps(verdicts, indent=2), encoding="utf-8"
    )

    # Summary
    failed = [v for v in verdicts if v["verdict"] != "VERIFIED"]
    total_cost = sum(v.get("cost_usd", 0.0) for v in verdicts)
    print(
        f"[session10-gate] done. "
        f"{len(verdicts) - len(failed)}/{len(verdicts)} VERIFIED. "
        f"Total gate cost: ${total_cost:.4f}",
        flush=True,
    )
    if failed:
        print("[session10-gate] ISSUES_FOUND:", flush=True)
        for v in failed:
            print(f"  [{v['index']+1}] {v['label']}: {v.get('detail') or v.get('raw', '')[:400]}", flush=True)
        raise SystemExit(1)

    print("[session10-gate] uat-coverage-review: VERIFIED for every item.", flush=True)


if __name__ == "__main__":
    main()
