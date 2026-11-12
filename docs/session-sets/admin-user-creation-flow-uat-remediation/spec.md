# admin-user-creation-flow-uat-remediation

> **Purpose:** Fix two blocked UAT items from the `admin-user-creation-flow` UAT run.
> **Created:** 2026-05-11
> **Prerequisite set:** `admin-user-creation-flow` (UAT-1 PASS, UAT-3 PASS; UAT-2 BLOCKED, UAT-4 BLOCKED)
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: true
requiresE2E: false
uatScope: partial
outsourceMode: first
```

Rationale: Only UAT-2 and UAT-4 need re-running. E2E Playwright tests already pass; no new Playwright work needed.

---

## UAT Review Disposition

| Finding | Classification | Root cause | Action |
|---|---|---|---|
| UAT-2 BLOCKED: `manager@acmecoffee.com` not in dev email inbox | **Substantive** | Seed data gap — no seeded UserAdmin persona | Fix seed data |
| UAT-4 BLOCKED: can't create user (stale DB state) | **Minor** | `RequiresAppStateReset: true` only applies to W2 runner, not manual UAT; prior Playwright run left `audit-test@test.local` in DB | Update checklist email to a less collision-prone value; document reset precondition |
| 4 "Other" items (empty, Passes=False) | **Out of scope** | Auto-generated empty rows from checklist editor | No action |

---

## Session 1 of 1: Seed data fix + checklist update + re-UAT

**Steps:**

1. Add `manager@acmecoffee.com` to `SeedData.cs` as a seeded backoffice user with the `UserAdmin` baked-in role scoped to the Internal Operations org. The persona should follow the same pattern as `admin@acmecoffee.com` (already seeded, registered, and addressable via magic-link dev flow).

2. Update the UAT checklist:
   - UAT-4 step 10: change email from `"audit-test@test.local"` to `"audit-uat-v2@test.local"` (avoids collision with any prior run's `audit-test@test.local`).
   - Add a `Feedback` note to UAT-4 stating: "If you see 'A user with this email already exists', restart the docker app containers (docker compose -f docker/docker-compose.app.yml up --force-recreate -d) to reset database state."

3. Rebuild docker containers to pick up seed data change.

4. Confirm `manager@acmecoffee.com` receives a magic-link email when requested via `/login`.

5. Route cross-provider verification of seed data change.

6. Commit, push, mark complete, notify.

**Creates:**
- Updated `src/Apps/AcmeCoffeeOnline.Api/Data/SeedData.cs`
- Updated `docs/session-sets/admin-user-creation-flow/admin-user-creation-flow-uat-checklist.json`

**Ends with:** Seed data fixed; updated checklist committed; human can re-run UAT-2 and UAT-4.

**Progress keys:** `session-1/seed-data-fix`, `session-1/checklist-update`, `session-1/build-verify`, `session-1/verification`

---

## Re-UAT scope

After this set closes, human runs ONLY the two blocked items:
- UAT-2 (UserAdmin role dropdown restriction) — sign in as `manager@acmecoffee.com`
- UAT-4 (audit emission) — use the updated email after DB reset
