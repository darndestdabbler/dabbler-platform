# Verification Round 1

**ISSUES_FOUND**

---

### Issue 1: Session 3 Mask Exemptions sub-tab depends on Session 4 Default Masks data
- **Severity:** Major
- **Category:** Session Sequencing
- **Details:** Session 3 builds `MaskExemptionsSubTab.razor` whose cascading dropdowns are "sourced from `SecurityConfiguration.DefaultMasks` (so exemptions can only target declared-sensitive columns)" (Session 3, Step 8). However, the Default Masks tab — the UI that *populates* `SecurityConfiguration.DefaultMasks` — is not built until Session 4. This creates a circular dependency or, at minimum, a degraded experience: when Session 3 ships, the Mask Exemptions sub-tab's cascading dropdown will either be empty (no Default Mask rows exist yet because the tab to create them hasn't been built) or depend on manually seeded JSON data that the spec never mentions preparing.

  The spec does acknowledge cross-tab reactivity ("Mask Exemptions sub-tab in Role Master Details sees newly declared masks in the same circuit without a reload" is a Session 4 Ends-with criterion), but it doesn't address the Session 3 state where the Default Masks tab doesn't exist yet. Options: (a) swap Sessions 3 and 4 so Default Masks lands first, (b) seed at least one `DefaultMaskRule` in `security-config.json` during Session 3 so the sub-tab is testable, or (c) explicitly note in Session 3 that the Mask Exemptions sub-tab is structurally complete but functionally empty until Session 4 fills the source list, and defer its bUnit "add/edit" interaction tests to Session 4. The spec is silent on all three, which makes Session 3's Ends-with criterion ("authority rules enforced on Mask Exemptions") untestable against real data.

---

### Issue 2: `[Obsolete]` type deletion assigned to Set C in the Set A Follow-up but claimed by Set B in the spec — incomplete reconciliation
- **Severity:** Minor
- **Category:** Completeness / Correctness
- **Details:** The spec's "What Set A explicitly deferred to Set B (or later)" section, item 3, correctly notes: "Set A's Follow-up tagged this to 'Set C' but Set C's scope (per the roadmap) is transactional system columns, not security cleanup." The spec then claims progressive deletion across Sessions 2–8. However, the Set A change-log's Follow-up section literally says "Physical deletion of the legacy configuration types marked `[Obsolete]` (Set C)." The spec overrides this assignment without explicitly flagging it as a deviation from the Set A change-log's stated plan. Since the spec also says "Decisions confirmed with the human (do not re-litigate)," this reassignment should either appear in that decisions list or be marked as a correction of Set A's follow-up wording. As written, a future reader comparing the Set A change-log to the Set B spec will see a contradiction with no explicit resolution.

---

### Issue 3: Session 2 Step 5 deletion scope may be overambitious — `EfCoreSecurityStore` likely still references several `[Obsolete]` types
- **Severity:** Minor
- **Category:** Feasibility
- **Details:** Session 2, Step 5 proposes deleting all seven `[Obsolete]` configuration types (`PagePermission`, `EntityPermission`, `FieldPermission`, `ControlPermission`, `EndpointPermission`, `RowFilter`, `RoleSwitchingPermission`) once their "last UI consumer" is gone. Step 6 then deletes the matching EF entities and their lines in `EfCoreSecurityStore`, `DabblerSecurityDbContext`, and `ModelBuilderExtensions`. The spec hedges correctly ("If any still compiles against live code... keep them until the dependent file is itself removed. Record in the session log which deletions landed and which stayed."), but the Steps, Creates/Touches, Deletes, and Ends-with sections all frame the *expected* outcome as "all seven deleted." Given that `EfCoreSecurityStore` is a persistence layer that likely implements load/save for these types (and may be consumed by non-UI paths like API endpoints or background services the spec doesn't inventory), the realistic outcome is probably that most types survive Session 2 and trickle out in Sessions 3–7 as their full dependency graphs are cleared. The spec should set expectations more accurately so Session 2's Ends-with doesn't become misleading. The hedge language is good but it's buried in Step 5; the Ends-with should echo it.

---

### Issue 4: Architecture doc specifies baked-in roles show Mask Exemptions sub-tab only, but spec also says "read-only panel summarizing compiled privileges" — no issue, but `BakedInPrivilegesPanel` is not mentioned in Session 3's bUnit test list
- **Severity:** Minor
- **Category:** Completeness
- **Details:** Session 3, Step 9 says "Write bUnit interaction tests: filter, add, edit, delete, baked-in vs. user-type row rendering, authority-rule enforcement." The `BakedInPrivilegesPanel.razor` (built in Step 4) gets no explicit test call-out. Since the panel is read-only and sourced from `BakedInRolePrivileges`, a rendering/snapshot test is low-cost and should be listed to avoid it being silently skipped.

---

### Issue 5: Session 6 EF global query filter scope may conflict with existing manual org-filtering in ways that break tests
- **Severity:** Minor
- **Category:** Risk
- **Details:** Session 6, Step 3 applies a global EF query filter to "every transactional `DbSet`" in `AcmeCoffeeDbContext`. Step 3 also says "Escape the filter for users with SystemAdmin, CrossOrgUserAdmin, or CrossOrgMonitor." The risk is that existing integration tests (including the 20+ tests from Set A's Sessions 4–5) likely run without an authenticated user context or with a test context that doesn't set an active organization. A global query filter that reads from `IHttpContextAccessor` or a scoped service will either throw or silently return zero rows in test contexts that don't set up the org. The Risk Notes section doesn't mention this; Session 6's Steps mention "(expect existing cross-org tests to need small updates)" but this underestimates the blast radius of a *global* query filter applied to *every* transactional DbSet. This deserves a risk note on par with the Session 5 size risk.

---

### Issue 6: Exit criteria mention "Playwright coverage for the new dashboard added" but Session 8 Steps don't include a Playwright step
- **Severity:** Minor
- **Category:** Completeness
- **Details:** The whole-set Exit Criteria state: "Playwright coverage for the new dashboard added (tab navigation, per-role gating, restriction grids, safety net, reference-integration field behavior)." However, Session 8's Steps list UAT checklist creation, change-log generation, Role Documentation tab, Step 9 reorganization review — no step writes Playwright tests. The Risk Notes mention "the new dashboard gains its own Playwright coverage by Session 8 at the latest" and "Partial Playwright breakage is expected between Sessions 2 and 8," but no session between 2 and 8 has an explicit "write Playwright tests for the new tabs" step either. The Playwright coverage is an orphaned exit criterion with no session owning its delivery.

---

### Issue 7: `PermissionResolver.cs` deletion mentioned in "Files deleted progressively" but never assigned to a session
- **Severity:** Minor
- **Category:** Completeness
- **Details:** The Architecture Context's "Files deleted progressively" section lists `Authorization/PermissionResolver.cs` with "if it only serves legacy handlers (confirm during Session 2)." Session 2's deletion list (Step 4) doesn't include it. No subsequent session mentions it. If Session 2 confirms it should be deleted, no session claims the deletion. If it's kept, no session marks it as retained. This file falls through the cracks of the per-session tracking.

---

### Issue 8: Session 1 as spec-only session is appropriate but creates an 8-session set where 7 would suffice
- **Severity:** Minor
- **Category:** Efficiency
- **Details:** Using Session 1 solely for spec authorship is defensible — it gave the human a review checkpoint before code changes. The downside is marginal: one extra session boundary, one extra build+test cycle, one extra verification round, and $0.15–0.25 in routing cost for a session that produced no executable change. Given the spec's own risk note about Session 5 potentially needing a 5b, an 8-session plan leaves less room before the session count becomes unwieldy (9+ sessions in a set is unusual per the workflow patterns in the attached docs). This is a design tradeoff, not a defect.

---

### Summary

The spec is well-structured and substantively complete. The one **Major** issue is the Session 3 → Session 4 ordering dependency on Default Masks data for the Mask Exemptions sub-tab. The remaining issues are **Minor** and mostly about tightening expectations, adding missing test/Playwright steps, and ensuring orphaned deletion targets get assigned to sessions.

---

# Verification Round 2

# Verification Round 2

## Fix-by-fix verification

### R1-001 (Major): Session 3 → Session 4 data dependency

**VERIFIED.** Session 3 Step 9 seeds a `DefaultMaskRule` (`Customer.SocialSecurityNumber` with `MaskFormat = "XXX-XX-{last4}"`) into `security-config.json`. The Ends-with criterion is scoped: "structurally complete and operates against the seeded row; richer interaction coverage (add-many, filter-across-many) lands in Session 4." The `security-config.json` files are added to Session 3's Touches list. Progress key `session-3/seed-default-mask-rule` is present. The cascading dropdown in `MaskExemptionsSubTab` now has at least one row to target, making Session 3's authority-rule tests executable without Session 4. Correctly resolved.

### R1-002 (Minor): Set A reassignment flagging

**VERIFIED.** The "What Set A explicitly deferred to Set B (or later)" item 3 now explicitly says: "**This spec treats the Set A change-log's '(Set C)' tag as a correction candidate and reassigns the deletion to Set B, progressively, alongside the UI consumers.**" It states the rationale, acknowledges the original wording, and points to Session 8 Step 9 as the human's opportunity to weigh in. A future reader comparing the Set A change-log to this spec will find the contradiction explicitly called out and justified. Correctly resolved.

### R1-003 (Minor): Session 2 Ends-with hedge

**VERIFIED.** Session 2's Ends-with now reads: "The `[Obsolete]` type deletions are best-effort — as many as can cleanly cascade alongside the UI removals land here; any type whose non-UI consumers (EF store, handler Requirements, API endpoints, background services) still reference it survives to the next session that removes that consumer. The session log enumerates what actually landed vs. what stayed." This echoes and strengthens the Step 5 hedge language. Correctly resolved.

### R1-004 (Minor): BakedInPrivilegesPanel in bUnit test list

**VERIFIED.** Session 3 Step 10 now reads: "Write bUnit interaction tests — including for `BakedInPrivilegesPanel.razor` (read-only rendering + alias-override display): filter, add, edit, delete, baked-in vs. user-type row rendering, baked-in privileges panel content, authority-rule enforcement on Mask Exemptions." The panel is explicitly named. Correctly resolved.

### R1-005 (Minor): Session 6 EF query filter risk note

**VERIFIED.** A new risk note appears in the Risk Notes section: "**Session 6 global EF query filter blast radius.** Adding `HasQueryFilter` to every transactional `DbSet` will affect every integration test that touches one of those entities — including the 20+ tests Set A Sessions 4–5 added, the `OrderService` tests, Customer/Product tests, and API controller tests." It prescribes `IOrganizationContextAccessor` test double / `TestAuthSessionBuilder` helper and baked-in role escape verification. The level of detail is on par with the Session 5 size risk, as the round 1 finding requested. Correctly resolved.

### R1-006 (Minor): Playwright coverage ownership

**VERIFIED.** Playwright delivery is now assigned to four sessions:
- **Session 2, Step 10:** Five-tab smoke test + role gating.
- **Session 5, Step 8:** Restriction form/grid reference integration.
- **Session 7, Step 7:** Role-assignment workflow.
- **Session 8, Step 4:** Full dashboard sweep (arrow-key nav, safety-net flow, Role Documentation rendering).

Progress keys added: `session-2/playwright-five-tab-smoke`, `session-5/playwright-restriction-coverage`, `session-7/playwright-role-assignment`, `session-8/playwright-full-dashboard-sweep`. The exit criterion ("Playwright coverage for the new dashboard added (tab navigation, per-role gating, restriction grids, safety net, reference-integration field behavior)") now maps cleanly to these four sessions' explicit steps. No longer orphaned. Correctly resolved.

### R1-007 (Minor): PermissionResolver.cs fate

**VERIFIED.** Session 2 Step 7a explicitly addresses `PermissionResolver.cs`: "Confirm the status of `PermissionResolver.cs`: grep its references after Steps 4–7 land. If the legacy handlers and matrix components were its only consumers (the most likely outcome), delete it in this session. If any non-legacy consumer still uses it, retain it and note the surviving consumer in the session log for a follow-up session to address. Either way, the file's fate is recorded here and does not carry forward as an orphan deletion target." Progress key `session-2/delete-permission-resolver` is in Session 2's list. Correctly resolved.

## Check for newly introduced issues

Reviewed the full updated spec for:
- **Contradictions:** None found. The seeded `DefaultMaskRule` in Session 3 is consistent with Session 4's role of building the *tab* that manages the full list. Session 3's Ends-with correctly defers richer coverage to Session 4.
- **Broken session dependencies:** None. The seed in Session 3 Step 9 is self-contained (editing `security-config.json` directly, not depending on a UI that doesn't exist yet).
- **Missing progress keys:** All new progress keys are present (`session-3/seed-default-mask-rule`, `session-2/delete-permission-resolver`, `session-2/playwright-five-tab-smoke`, `session-5/playwright-restriction-coverage`, `session-7/playwright-role-assignment`, `session-8/playwright-full-dashboard-sweep`).
- **Step numbering:** Session 3's steps run 1–13 after the insertion of Step 9 (seed) and renumbering of the bUnit step to 10. Consistent.
- **Session 2 step numbering:** Steps go 1–12, with 7a inserted between 7 and 8. This is fine — the "a" suffix is clear and doesn't break the numbering.
- **Touches list consistency:** Session 3 Touches now includes `security-config.json` for both Web and Api copies. Consistent with Step 9.
- **Risk note duplication:** The Session 6 risk note in the Risk Notes section vs. the inline mention in Session 6 Steps ("expect existing cross-org tests to need small updates") are complementary, not contradictory. The risk note provides the detail the round 1 finding asked for.

No new issues introduced.

---

**VERIFIED**
