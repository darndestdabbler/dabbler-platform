# Session 001 Cross-Provider Verification

## Round 1 — gpt-5.4 (openai) — ISSUES_FOUND

---MODEL--- gpt-5.4 (openai)
---TOKENS--- in=1467, out=4354
---COST--- $0.0690
---CONTENT---
```json
{
  "verdict": "ISSUES_FOUND",
  "summary": "Closure not adequate. Manual SSN backfill not codified; speculative role-switch header trusted without API-side authorization; UAT-3 evidence missing for DSL pipeline; alleged contradiction in PermissionService masking.",
  "issues": [
    {"severity": "Critical", "title": "X-App-Switched-Role trusted without API-side authorization"},
    {"severity": "Major", "title": "SSN backfill manual, not codified"},
    {"severity": "Major", "title": "No evidence UAT-3 passed in DSL/Playwright pipeline (later determined to be a verifier misread; W2 had passed)"},
    {"severity": "Major", "title": "Alleged contradiction in PermissionService masking (later determined to be a verifier misread; the DB has zero AppFieldPermission rows for Customer.SSN, so the null-out path is never reached)"}
  ]
}
```

## Round 2 — gpt-5.4 (openai) — ISSUES_FOUND

---MODEL--- gpt-5.4 (openai)
---TOKENS--- in=1573, out=3876
---COST--- $0.0654
---CONTENT---
```json
{
  "verdict": "ISSUES_FOUND",
  "summary": "Most of round 1 addressed, but the gate appeared to skip the AllowedRoles intersection.",
  "issues": [
    {"severity": "Major", "title": "Switched-role authorization appears to omit the configured allowed-role gate (the round-2 prompt described natural-roles only; AllowedRoles intersection had been ADDED in code but not yet reflected in the prompt)"}
  ]
}
```

After round 2, follow-on diagnostic work revealed the actual root cause of the W2 test flake was NOT the speculative gate's per-request EnsureLoadedAsync latency, NOR the RestrictedFormField initial-render race (though that was a real defense-in-depth gap that got fixed). The actual root cause was DETERMINISTIC state pollution across W2 runs: UAT-2 grants a MaskExemption that SecurityConfigurationManager persists to the container's writable /app/security-config.json, and the next run's UAT-1 fails because the exemption is still there. The speculative gate work was reverted in full; the correct fix was a `--reset-app-state` flag in the runner plus a `RequiresAppStateReset: true` field on the checklist JSON that auto-triggers the reset.

## Round 3 — gpt-5.4 (openai) — ISSUES_FOUND

---MODEL--- gpt-5.4 (openai)
---TOKENS--- in=1690, out=4255
---COST--- $0.0869
---CONTENT---
```json
{
  "verdict": "ISSUES_FOUND",
  "summary": "Direction looks plausible; reverts and codified backfill are correct. But state-reset fix was opt-in via CLI flag only — no automation guarantees the canonical run path uses it.",
  "issues": [
    {"severity": "Major", "title": "State-reset fix not integrated into the canonical Session 1 run path"},
    {"severity": "Minor", "title": "Closure claim stronger than evidence (W2 still has a known popover-timeout flake)"}
  ]
}
```

## Round 3 follow-on (resolved without re-routing)

The Major issue from round 3 was addressed by:
1. Adding a top-level `RequiresAppStateReset: true` field to the checklist JSON.
2. Updating the runner's `cli.py` to read that field and auto-reset whenever it's set, regardless of CLI flag.
3. Documenting the field as the canonical signal in `project-guidance.md`.

Empirical verification: 5 consecutive runs of `python ai-router/uat_runner/cli.py ... --skip-triage` (NO `--reset-app-state` flag) auto-reset containers via the checklist field and ran cleanly. 4/5 passed; 1/5 hit the pre-existing OPEN DROPDOWN popover-timeout flake (a separate W0-emitter race, scoped for the follow-on `uat-dsl-verify-input-value` session set).

The Minor wording concern was acknowledged: this session's CODE CHANGES are acceptable; the W2 PIPELINE has a known pre-existing flake that is out of scope for this session set.

## Final state

- 715 non-Playwright unit tests pass.
- Speculative role-switch propagation REVERTED in full.
- DatabaseInitializer codifies the SSN backfill.
- RestrictedFormField initial-render race fixed (defense-in-depth).
- W2 runner auto-resets containers when the checklist declares `RequiresAppStateReset: true`.
- Two new lessons in `lessons-learned.md` (initial-render contract for restriction binders; container-writable-layer state pollution).
- Catalog-classifier convention promoted to `project-guidance.md`.
- DSL-verb follow-on proposal at `docs/session-sets/uat-dsl-verify-input-value/spec.md` covers VERIFY INPUT value-match AND OPEN DROPDOWN timing tightening.
- Human ran UAT-1..6 manually and confirmed all 6 pass.

Session 1 close approved.
