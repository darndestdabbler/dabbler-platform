# Sensitive-Field Masking and Audit

> **Purpose:** Wire `DefaultMaskRule` + `MaskExemption` (with
> time-bound `ExpiresAt` and separation-of-duties authorization)
> into live data rendering. Emit audit rows for role switches,
> unmasked reads of sensitive columns, exemption grants, and
> exemption expirations. Provide a compliance-review viewer.
> **Created:** 2026-04-19 *(stub — fleshed out at start of Session 1)*
> **Session Set:** `docs/session-sets/sensitive-field-masking-audit/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Architecture doc:** [../rearchitecting-role-administration/rearchitecting-role-administration.md](../rearchitecting-role-administration/rearchitecting-role-administration.md)
> **Prerequisite:** Set A and Set B both complete. Set B's
> enforcement seam must be in place so this set can plug in the
> mask-resolution service.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**This is a stub spec.** The authoritative session breakdown is in
the roadmap file under "Session Set D — Sensitive-Field Masking
and Audit." Before running Session 1, the orchestrator should
flesh this spec out with detailed step lists per session, progress
keys, and updated risk notes.

## Goals (from the roadmap)

1. `IFieldMaskService` resolves (default-mask, exemption) →
   masked-or-unmasked render. Handles null values (mask still
   shown) and applies distinct visual treatment.
2. Separation-of-duties authorization enforced in the domain
   service that writes `MaskExemption` rows and mirrored in the
   dashboard UI.
3. `AppSensitiveAccessLog` temporal entity with event kinds
   `RoleSwitch | FieldUnmaskedRead | ExemptionGranted |
   ExemptionExpired`. Correlated per-render via `CorrelationId`.
4. Compliance Review viewer (6th dashboard tab OR
   `/admin/sensitive-access-log` page — decided during session 1).
   `FilterPanel<T>` over user, date range, organization, entity,
   column, event kind; read-only; CSV export.
5. UAT checklist exercises the full path:
   `Customer.SocialSecurityNumber` masked for Customer role,
   unmasked after switch to a role with a time-bound exemption,
   with both events visible in the audit viewer.

## Session Plan (brief — fleshed out at session 1)

1. `IFieldMaskService` + mask rendering + null-data handling +
   masked-control visual treatment. Decide Compliance Review
   placement (in-dashboard tab vs. standalone page).
2. Exemption authorization rules + role-switch audit schema
   (`AppSensitiveAccessLog` with the four event kinds, the
   `IRoleSwitcher` hook, the exemption-write hook, the
   query-time expiry detection).
3. Unmasked-read audit interception with per-render
   `CorrelationId` batching.
4. Compliance review viewer + CSV export.
5. UAT + docs + change log + Step 9 reorganization.

Expect 4–5 sessions.
