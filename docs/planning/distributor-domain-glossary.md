# Distributor Domain Glossary

> **Purpose:** Freeze the canonical Phase 2 vocabulary for the
> `distributor-reframe` session set before app, seed, and documentation updates
> change the sample-app story.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/notes-driven-app-acceptance-baseline.md`,
> `docs/session-sets/stubbed-uat/early-uat-findings.json`,
> `docs/session-sets/stubbed-uat/change-log.md`, and
> `docs/session-sets/distributor-reframe/spec.md`

---

## Review Scan Summary

Session 1 reviewed the current accepted app baseline, the completed
`stubbed-uat` findings and closeout, and the remaining Phase 2 session-set
specs before reframe implementation begins.

The review confirmed that:

- the sample app still presents the older `AcmeCoffeeOnline` story in app copy,
  dashboard labels, and the accepted `/platform/stub` route
- the completed `stubbed-uat` set already triaged the early design and
  navigation issues that would block the reframe from starting
- the remaining Phase 2 specs already expect a distributor-first handoff and do
  not require textual changes before this set proceeds

## Canonical Terms

| Term | Canonical meaning | Notes for upcoming sessions |
| --- | --- | --- |
| `AcmeCoffeeDistributors` | The sample application's canonical business identity. | Use this for user-facing titles, guidance, and seeded-story narration. |
| `Retail business` | A coffee shop, cafe, grocer, or similar business that buys from `AcmeCoffeeDistributors`. | Treat this as the business-facing term in copy and workflow descriptions. |
| `Organization` | The technical entity that represents either a retail business or the internal organization. | Keep this as the implementation-facing term in architecture and security work. |
| `Internal organization` | The configurable default organization used for internal staff and admin contexts. | This stays visible in planning now and becomes concrete in `multi-org-security`. |
| `Internal staff` | Employees of `AcmeCoffeeDistributors` who work in the internal organization. | Prefer this user-facing phrase over legacy backoffice wording. |
| `Retail-business user` | A user who belongs to one retail-business organization. | This is the baseline external persona for later security and workflow proof. |
| `Regional sales rep` | An internal-staff persona with organization-scoped admin access across multiple retail businesses. | This is a persona, not a separate security role. |
| `SystemAdmin` | A global cross-organization administrator role. | Keep the existing technical name. |
| `UserAdmin` | The future organization-scoped user-administration role. | Use this in planning and acceptance language for the post-reframe target model. |
| `ViewOrders` / `MakeOrders` | Future organization-scoped retail-business roles. | Reserve these names for the multi-org implementation set. |
| `/platform/stub` distributor demo | The accepted app-hosted stub route that demonstrates the reusable platform through a distributor-to-retail-business story. | Reframe its sample data, tab labels, and guidance with the main app instead of leaving it on legacy naming. |
| `Customer` | A legacy app term that now maps to a retail business in the user-facing story. | Keep the current technical shape where needed until the later security and app-coverage sets harden the deeper model. |

## Naming Guardrails

- Favor distributor-story copy in page titles, dashboard cards, quick links,
  sample data, and reviewer guidance.
- Avoid solution-wide namespace, project-file, or folder renames in this set
  unless later implementation work proves they are unavoidable.
- Keep the implementation-facing `Organization` term available for later
  security work even when the UI copy says `retail business`.
- Treat `/platform/stub` as part of the same sample-app narrative, not as a
  separate legacy demo that can keep old terms.

## Queue And Parallelization Verdict

No downstream Phase 2 spec edits are required after this review. The remaining
specs already depend on the accepted distributor baseline and correctly defer
their deeper work until this set is complete.

No new cross-set overlap is safe. `multi-org-security` and every later Phase 2
set still depends on the accepted distributor baseline that closes in Session 3
of `distributor-reframe`.

Inside this set, a narrow intra-set overlap is acceptable after the glossary is
frozen:

- app, seed, and navigation-copy reframe work may proceed in parallel with
  test and documentation cleanup only when the edits stay on disjoint files and
  the distributor glossary remains the merge source of truth

## Session 2 Watchpoints

- Reframe `/platform/stub` and its seeded sample story alongside the main app
  so the accepted demo path does not drift behind.
- Update hard-coded dashboard configuration carefully because entity names and
  labels are embedded in generated configuration objects rather than isolated in
  plain markup.
- Preserve the platform-first architecture while reworking user-facing story
  copy; this set should not quietly become a full security-model rewrite.
