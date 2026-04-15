# Distributor Reframe Migration Map

> **Purpose:** Map the current legacy `AcmeCoffeeOnline` narrative to the
> accepted `AcmeCoffeeDistributors` story so Session 2 can update app copy,
> seed content, and guidance without losing the platform-first baseline.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/distributor-domain-glossary.md`,
> `docs/planning/notes-driven-app-acceptance-baseline.md`,
> `docs/session-sets/stubbed-uat/early-uat-findings.json`, and
> `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Dashboard.razor`

---

## Migration Principles

- Reframe the sample-app story first; do not force broad technical renames
  during Session 1.
- Keep project names, namespaces, and other stable implementation identifiers
  unless later implementation work shows a strong reason to change them.
- Distinguish user-facing vocabulary from implementation-facing architecture:
  the UI may speak about retail businesses while the platform still uses
  `Organization` as the technical concept.
- Bring the accepted `/platform/stub` route forward with the same distributor
  narrative as the main app.

## Legacy-To-Distributor Mapping

| Legacy term or assumption | Distributor baseline | Session guidance |
| --- | --- | --- |
| `AcmeCoffeeOnline` | `AcmeCoffeeDistributors` | Update page titles, headings, navigation text, reviewer guidance, and sample-story copy to the distributor identity. |
| `customer` as the primary business story | `retail business` as the customer of the distributor | Use `retail business` in UI copy and guidance where the user is reading business-language labels. |
| `Customer` as the current data surface | current app customer records become the first distributor-era retail-business records | Reframe the story around the existing surface first; deeper organization-aware reshaping belongs to later sets. |
| `Customers` dashboard and navigation labels | `Retail Businesses` | Update visible labels while preserving the current routed proof surfaces. |
| `customer search` | `retail-business search` | Reframe page copy, sample filters, and stub/demo labels to match the new story. |
| `customer detail` | `retail-business detail` or `retail account detail` | Favor wording that keeps the business relationship clear without implying consumer accounts. |
| `backoffice` language | `internal staff` or `internal operations` | Move user-facing copy away from backoffice wording. |
| `BackofficeUser` and `BackofficeUserAdmin` technical roles | keep technical identifiers for now; describe them as internal-staff access in user-facing copy | The multi-org session will freeze the future role model; do not over-rename code prematurely here. |
| generic admin story | `internal organization` plus later organization-scoped administration | Keep the internal-organization concept visible in planning and copy where it helps frame later security work. |
| retail-customer sample persona names | seeded retail-business contacts and internal-staff personas | Update sample names and organization stories so later multi-org seeds have a coherent starting point. |
| `/platform/stub` as a generic stub demo | `/platform/stub` as the distributor demo path for reusable platform proof | Reframe tab labels, alerts, sample records, and reviewer instructions with the same distributor vocabulary used in the main app. |
| `Order` workflows | distributor orders placed by users from retail businesses | Keep the entity name, but make the business relationship explicit in descriptions and sample data. |
| `Product` workflows | distributor product catalog and availability workflows | Keep the entity name; update surrounding narration to the distributor story. |
| accepted app proof harness | accepted distributor proof harness | Preserve the current platform-first architecture while changing the story it demonstrates. |

## Terms That Stay Unchanged For Now

- `Organization` remains the architecture-facing term used by later
  `multi-org-security` work.
- `SystemAdmin` remains the cross-organization technical role name.
- Existing solution, project, and namespace identifiers remain in place unless
  later implementation work proves a rename is necessary.
