# AI Assignment: sensitive-field-masking-audit-uat-remediation

> **Authoring note (2026-04-30):** The human's session-trigger explicitly
> said "do not route to an external provider, except for the cross-provider
> verification" and named Gemini Pro as the verifier. Per the precedence
> rule in `project-guidance.md` (explicit human direction in the current
> conversation > the rule that ai-assignment.md must be routed), the
> Session 1 redo authors this file directly. Routed analysis for the
> Session 2 forward recommendation will resume at end-of-Session-1 unless
> the human extends the override.

## Session 1 of 2: Real-login UAT remediation (redo under DSL pipeline)

### Recommended orchestrator

`claude-code claude-opus-4-7 @ effort=high` (1M context).

### Rationale

This session needs (a) deep familiarity with the DSL pipeline mechanics
the `uat-workflow-dsl-alignment` set just shipped, (b) careful catalog
grounding for label-verbatim authoring, and (c) the ability to read a
multi-file diff (user-roles.json, SeedData.cs, UserContextBar, the
component catalog, the W0 emitter output, the W2 runner classifications)
without context starvation. The 1M context window is the lever that lets
a single orchestrator carry the iterate-to-green W2 loop without
re-loading state between sub-steps.

### Estimated routed cost

Low (the human capped routing for this run; the only routed call is
session-verification). Without the cap, this session would normally
spend ~$0.30–$0.60 across `uat-script-author` (Sonnet by default) +
session-verification (Gemini Pro per the override).

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct (orchestrator owns mechanics) |
| 2 | Confirm restore-devidp prerequisite | Direct (file existence check) |
| 3 | Assign 3 roles in user-roles.json + verify role switcher | Direct (single-file edits, deterministic) |
| 4 | Author DSL UAT checklist | Direct (human override; would normally be `uat-script-author`) |
| 5 | W0 compile (`dabbler-uat-dsl compile`) | Direct (CLI invocation) |
| 6 | W2 runner iterate-to-green | Direct (CLI + targeted fixes) |
| 7 | Full Playwright suite + idempotency 2× | Direct (test execution) |
| 8 | Delete legacy `SensitiveFieldMaskingUatCoverageTests.cs` | Direct (file delete) |
| 9 | Full non-Playwright dotnet test suite | Direct |
| 10 | Cross-provider session-verification | **Routed → Gemini Pro** (Rule #2) |
| 11 | Commit, push, notify human | Direct |

### Actuals (filled after the session)

- Orchestrator used: claude-code claude-opus-4-7 @ effort=high (1M context)
- Total routed cost: ~$0.006 for the in-loop W2 triage call that auto-recovered
  on rerun; cross-provider session-verification cost recorded at Step 10.
- Deviations from recommendation: Human override — only cross-provider
  verification was routed; everything else was authored directly. Per
  memory `feedback_self_verify_when_no_cross_provider.md` this would
  normally still be cross-provider since Gemini Pro is available; the
  human pinned Gemini Pro explicitly.
- Mid-session scope expansion: UAT-3's empty-input feedback led down
  a multi-stage diagnostic path. The initial hypothesis (a two-permission-
  systems mismatch between `AppFieldPermission Hidden` and `MaskExemption`)
  did not hold — the DB has zero `AppFieldPermission` rows for
  `Customer.SocialSecurityNumber`. The actual immediate cause was a NULL
  data row (James Smith's SSN) that the schema's idempotent ALTER never
  backfilled. **Codifying that backfill in
  `DatabaseInitializer.EnsureAcmeCoffeeSchemaAsync` was the real UAT-3
  fix.** A follow-on session-verification flagged a separate issue — the
  speculative `X-App-Switched-Role` header forwarding I had added during
  diagnosis was unauthenticated on the API side. While hardening that
  gate, the W2 pipeline started flaking 2/3 on `VERIFY TEXT contains
  XXX-XX-`. A diagnostic page-state snapshot proved the page was
  rendering an editable `<input>` (count=1) instead of the masked `<span>`
  at the moment of failure — which `body.innerText` doesn't capture
  because input values aren't part of innerText. **Root cause:
  `RestrictedFormField` initialised its resolution state with
  `Unrestricted`, so the first render cycle showed the unrestricted
  `ChildContent` before the async `ResolveAsync` completed.** That flash
  was a real defense-in-depth gap for users on slow renders, not just
  a test artifact. The session reverted the speculative role-switch
  propagation in full and shipped the structural fix to
  `RestrictedFormField` (render nothing until `_isResolved`). 715 unit
  tests still pass.
- Notes for next-session calibration: the catalog crawler's
  visible-text-vs-aria-label classification and the
  render-nothing-until-resolved pattern for restriction-binder components
  are durable Step 9 reorg-review additions. The aborted role-switch-
  propagation work is a future security-hardening session set, to be
  reintroduced when an actual `AppFieldPermission Hidden` row needs
  API-side enforcement against role-switched users.

**Next-session orchestrator recommendation (Session 2):**
<routed at Session 1 closeout per Rule #17 once the override window closes>

---

## Session 2 of 2: User Administration vs. Users-and-Role-Assignment audit

To be authored at end of Session 1 via routed analysis (per Rule #17),
unless the override-no-routing window extends to that session as well.
