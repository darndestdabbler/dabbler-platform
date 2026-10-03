# AI Assignment — uat-dsl-verify-input-value

---

## Session 1: Implement `VERIFY INPUT` verb and `OPEN DROPDOWN` timing fix

### Recommended orchestrator
`claude-code claude-sonnet-4-6` @ effort=`high`

### Rationale
The session is highly mechanical — adding new DSL verbs by following well-established
patterns for grammar, AST, validation, emission, and unit tests. The only reasoning-heavy
step is session-verification (cross-provider, mandatory). Documentation update is routed
to the cheapest capable model.

### Estimated routed cost
low

| Step | Action | Routing Decision |
|------|--------|------------------|
| Grammar + AST + visitor | Add HAS/VALUE/MATCHING/NO tokens, verifyInputStep rule, two AST records, visitor method | direct (mechanical, <50 lines) |
| Validator probes | Add Input probes for VerifyInputValueMatchingStep + VerifyInputNoValueStep | direct (mechanical, <10 lines) |
| Emitter cases | Add ToHaveValueAsync emit patterns for both verbs | direct (mechanical, <20 lines) |
| OPEN DROPDOWN timing fix | Replace single-click with 3-attempt retry loop | direct (mechanical, <40 lines) |
| Unit tests | Grammar parse +/−, AST canonical text, validator catalog hit/miss, emitter snapshots | direct (mechanical pattern-following, ~60 lines) |
| Language reference | Update docs/language-reference.md with new verb syntax | route(documentation) |
| Masking checklist | One-line edit to UAT-3 step 14 in sensitive-field-masking-audit-uat-remediation | direct (trivial) |
| Session verification | Cross-provider verify full implementation | route(session-verification, cross-provider) |

### Actuals (filled after the session)
- Orchestrator used: claude-code claude-sonnet-4-6 @ effort=high
- Total routed cost: TBD
- Deviations from recommendation: none
- Notes for next-session calibration: n/a (single-session set)

**Next-session orchestrator recommendation:** n/a (this is Session 1 of 1)
