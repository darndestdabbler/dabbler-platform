"""Append-only metrics log for the AI router.

Writes one JSON line per routed call to ai-router/router-metrics.jsonl
(global, cross-session-set). The log is deliberately simple:
  - one record per line, no wrapping array
  - additive schema — new fields can be added without breaking old lines
  - safe to read with `tail -f` or stream-process with jq

Schema per line:
  {
    "timestamp":        ISO8601 string (UTC)
    "session_set":      str or null
    "session_number":   int or null
    "call_type":        "route" | "verify" | "tiebreaker"
    "task_type":        str
    "model":            str  (the generator or verifier)
    "provider":         "anthropic" | "google" | "openai"
    "tier":             int
    "complexity_score": int or null
    "effort":           str or null   (Anthropic effort or OpenAI reasoning.effort)
    "thinking_on":      bool          (Anthropic adaptive / Gemini dynamic / OpenAI reasoning)
    "input_tokens":     int
    "output_tokens":    int
    "cost_usd":         float
    "elapsed_seconds":  float
    "escalated":        bool
    "stop_reason":      str
    # For verifier calls only:
    "verifier_of":      str or null   (the generator model this call verified)
    "verdict":          str or null   ("VERIFIED" or "ISSUES_FOUND")
    "issue_count":      int or null
  }

Analysis is done by reading the file; no query layer is needed for the
data volumes this workflow produces. See print_metrics_report().
"""

import json
import os
import datetime
from pathlib import Path
from typing import Optional

_THIS_DIR = Path(__file__).parent


def _log_path(config: dict) -> Path:
    """Resolve the metrics log file path."""
    tuning = config.get("_tuning", {}) or {}
    metrics_cfg = tuning.get("metrics", {}) or {}
    filename = metrics_cfg.get("log_filename", "router-metrics.jsonl")

    override = os.environ.get("AI_ROUTER_METRICS_PATH")
    if override:
        return Path(override)

    return _THIS_DIR / filename


def _metrics_enabled(config: dict) -> bool:
    tuning = config.get("_tuning", {}) or {}
    metrics_cfg = tuning.get("metrics", {}) or {}
    return bool(metrics_cfg.get("enabled", True))


def record_call(
    config: dict,
    *,
    call_type: str,               # "route" | "verify" | "tiebreaker"
    task_type: str,
    model: str,
    provider: str,
    tier: int,
    complexity_score: Optional[int],
    generation_params: dict,
    input_tokens: int,
    output_tokens: int,
    cost_usd: float,
    elapsed_seconds: float,
    escalated: bool,
    stop_reason: str,
    session_set: Optional[str] = None,
    session_number: Optional[int] = None,
    verifier_of: Optional[str] = None,
    verdict: Optional[str] = None,
    issue_count: Optional[int] = None,
) -> None:
    """Append a single record to the metrics log. Never raises — if
    writing fails (disk full, permission), we silently skip rather
    than breaking the routed call."""
    if not _metrics_enabled(config):
        return

    # Extract effort / thinking_on from whatever shape the provider uses
    effort = None
    thinking_on = False
    if provider == "anthropic":
        effort = generation_params.get("effort")
        thinking_on = bool(
            (generation_params.get("thinking") or {}).get("enabled")
        )
    elif provider == "google":
        # Gemini "effort" equivalent: level or the nonzero budget bit
        effort = generation_params.get("thinking_level")
        budget = generation_params.get("thinking_budget")
        thinking_on = (effort is not None) or (
            budget is not None and budget != 0
        )
    elif provider == "openai":
        effort = generation_params.get("reasoning_effort")
        thinking_on = effort not in (None, "none", "minimal")

    record = {
        "timestamp": datetime.datetime.now(
            datetime.timezone.utc
        ).isoformat(),
        "session_set": session_set,
        "session_number": session_number,
        "call_type": call_type,
        "task_type": task_type,
        "model": model,
        "provider": provider,
        "tier": tier,
        "complexity_score": complexity_score,
        "effort": effort,
        "thinking_on": thinking_on,
        "input_tokens": int(input_tokens),
        "output_tokens": int(output_tokens),
        "cost_usd": round(float(cost_usd), 6),
        "elapsed_seconds": round(float(elapsed_seconds), 3),
        "escalated": bool(escalated),
        "stop_reason": stop_reason,
        "verifier_of": verifier_of,
        "verdict": verdict,
        "issue_count": issue_count,
    }

    try:
        path = _log_path(config)
        path.parent.mkdir(parents=True, exist_ok=True)
        with open(path, "a", encoding="utf-8") as f:
            f.write(json.dumps(record) + "\n")
    except Exception:
        # Metrics are best-effort. Never break a routed call.
        pass


def load_metrics(config: dict) -> list[dict]:
    """Read every metrics record. Returns empty list if file missing."""
    path = _log_path(config)
    if not path.exists():
        return []

    records = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                records.append(json.loads(line))
            except json.JSONDecodeError:
                continue
    return records


def print_metrics_report(config: dict) -> None:
    """Print a human-readable summary of the metrics log to stdout."""
    records = load_metrics(config)
    if not records:
        print("(no metrics recorded yet — router-metrics.jsonl is empty "
              "or missing)")
        return

    # Header
    print("\n" + "=" * 68)
    print(f"AI ROUTER — METRICS REPORT  ({len(records)} calls logged)")
    print("=" * 68)

    # Overall totals
    total_cost = sum(r.get("cost_usd", 0) or 0 for r in records)
    total_input = sum(r.get("input_tokens", 0) or 0 for r in records)
    total_output = sum(r.get("output_tokens", 0) or 0 for r in records)
    print(f"Total cost:           ${total_cost:.4f}")
    print(f"Total input tokens:   {total_input:,}")
    print(f"Total output tokens:  {total_output:,}")

    # By model: call count, total cost, escalation rate
    print("\n--- By model ---")
    by_model: dict[str, dict] = {}
    for r in records:
        m = r.get("model", "?")
        slot = by_model.setdefault(m, {
            "calls": 0, "cost": 0.0, "escalated": 0,
            "provider": r.get("provider", "?"),
        })
        slot["calls"] += 1
        slot["cost"] += r.get("cost_usd", 0) or 0
        if r.get("escalated"):
            slot["escalated"] += 1

    hdr = f"  {'model':<18} {'provider':<11} {'calls':>6} " \
          f"{'cost':>10} {'esc%':>6}"
    print(hdr)
    print(f"  {'-'*18} {'-'*11} {'-'*6} {'-'*10} {'-'*6}")
    for m, s in sorted(by_model.items(), key=lambda kv: -kv[1]["cost"]):
        esc_pct = (100.0 * s["escalated"] / s["calls"]) if s["calls"] else 0
        print(f"  {m:<18} {s['provider']:<11} {s['calls']:>6} "
              f"${s['cost']:>8.4f} {esc_pct:>5.1f}%")

    # By task type: cost concentration and escalation signal
    print("\n--- By task type ---")
    by_task: dict[str, dict] = {}
    for r in records:
        t = r.get("task_type", "?")
        slot = by_task.setdefault(t, {
            "calls": 0, "cost": 0.0, "escalated": 0,
            "models_used": {},
        })
        slot["calls"] += 1
        slot["cost"] += r.get("cost_usd", 0) or 0
        if r.get("escalated"):
            slot["escalated"] += 1
        m = r.get("model", "?")
        slot["models_used"][m] = slot["models_used"].get(m, 0) + 1

    hdr2 = f"  {'task_type':<24} {'calls':>6} {'cost':>10} " \
           f"{'esc%':>6}  model distribution"
    print(hdr2)
    print(f"  {'-'*24} {'-'*6} {'-'*10} {'-'*6}  {'-'*36}")
    for t, s in sorted(by_task.items(), key=lambda kv: -kv[1]["cost"]):
        esc_pct = (100.0 * s["escalated"] / s["calls"]) if s["calls"] else 0
        dist = ", ".join(
            f"{m}:{n}" for m, n in sorted(
                s["models_used"].items(), key=lambda kv: -kv[1]
            )[:3]
        )
        print(f"  {t:<24} {s['calls']:>6} ${s['cost']:>8.4f} "
              f"{esc_pct:>5.1f}%  {dist}")

    # Verifier agreement: verification calls only
    v_records = [r for r in records if r.get("call_type") == "verify"]
    if v_records:
        print("\n--- Verifier agreement (session-end + auto-verify) ---")
        print(f"  Total verification calls: {len(v_records)}")
        verified = sum(1 for r in v_records if r.get("verdict") == "VERIFIED")
        issues = sum(1 for r in v_records
                     if r.get("verdict") == "ISSUES_FOUND")
        pct = (100.0 * verified / len(v_records)) if v_records else 0
        print(f"  VERIFIED:     {verified} ({pct:.1f}%)")
        print(f"  ISSUES_FOUND: {issues}")

        # Agreement by verifier model: how often each verifier passes
        by_verifier: dict[str, dict] = {}
        for r in v_records:
            vm = r.get("model", "?")
            slot = by_verifier.setdefault(vm, {"n": 0, "verified": 0})
            slot["n"] += 1
            if r.get("verdict") == "VERIFIED":
                slot["verified"] += 1
        print(f"\n  {'verifier':<18} {'calls':>6} {'pass%':>7}")
        print(f"  {'-'*18} {'-'*6} {'-'*7}")
        for vm, s in sorted(by_verifier.items(),
                            key=lambda kv: -kv[1]["n"]):
            rate = (100.0 * s["verified"] / s["n"]) if s["n"] else 0
            print(f"  {vm:<18} {s['n']:>6} {rate:>6.1f}%")

    # Session-set breakdown (last 5 distinct session sets)
    sets: dict[str, dict] = {}
    for r in records:
        ss = r.get("session_set")
        if not ss:
            continue
        slot = sets.setdefault(ss, {"calls": 0, "cost": 0.0})
        slot["calls"] += 1
        slot["cost"] += r.get("cost_usd", 0) or 0

    if sets:
        print("\n--- By session set ---")
        print(f"  {'session_set':<40} {'calls':>6} {'cost':>10}")
        print(f"  {'-'*40} {'-'*6} {'-'*10}")
        for ss, s in sorted(sets.items()):
            print(f"  {ss:<40} {s['calls']:>6} ${s['cost']:>8.4f}")

    print("=" * 68 + "\n")
