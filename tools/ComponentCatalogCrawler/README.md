# ComponentCatalogCrawler

Produces a runtime UI catalog of every user-visible interactive element in
`AcmeCoffeeOnline.Web`, keyed by `(routePath × role × userVisibleLabel)`,
that conforms to the W0 schema at
`../../../dabbler-uat-dsl/schema/component-catalog.json`.

The catalog is consumed by `Dabbler.Uat.Dsl`'s `SemanticValidator` so that
catalog-grounded UAT script authoring (Session 2 of the
`component-catalog-grounded-authorship` set) can verify every label in a
generated checklist against a real, currently-rendered surface.

## Prerequisites

- The Web and API hosts must be reachable. The PlaywrightFixture pattern
  (auto-launch `dotnet run` against the projects) is not replicated here;
  start them yourself before running the crawler.
- Either run via Docker compose (the platform default):
  ```
  docker compose -f docker/docker-compose.infra.yml up -d
  docker compose -f docker/docker-compose.app.yml   up --build -d
  # → Web at http://localhost:5010, API at http://localhost:5011
  ```
- Or run directly via dotnet (matches the launchSettings ports):
  ```
  cd src/Apps/AcmeCoffeeOnline.Api && dotnet run --launch-profile http   # → :5021
  cd src/Apps/AcmeCoffeeOnline.Web && dotnet run --launch-profile http   # → :5020
  ```
- Playwright browsers must be installed once:
  ```
  pwsh tools/ComponentCatalogCrawler/bin/Debug/net10.0/playwright.ps1 install chromium
  ```

## Run

From the repository root:

```bash
dotnet run --project tools/ComponentCatalogCrawler/ -- \
  --output docs/component-catalog \
  --base-url http://localhost:5020 \
  --api-base-url http://localhost:5021
```

The crawler:

1. Scans `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/**/*.razor` for
   `@page` directives. Dynamic segments (`{CustomerId:int}`) are resolved
   to seeded entity IDs by querying the API with the
   `X-Internal-Api-Key` header.
2. For each role × route, signs in via `/test-auth/seeded-session` (same
   shared-secret pattern as `PlaywrightFixture.SeededSignInAsync`).
3. Waits for `[data-testid="main-shell"]` to be visible.
4. Walks the DOM via an in-page JS evaluation, extracts each interactive
   element's role, accessible name, aria-label, test-id, html tag, and
   nearest dialog scope.
5. Maps DOM/ARIA roles to the W0 `elementRole` enum
   (Button, IconButton, Link, Tab, MenuItem, Input, Checkbox, Switch,
   Dropdown, Grid, Row, Column, Snackbar, Element, Text).
6. Before crawling, scans every `.razor` file in the pages directory for
   `@page` + `@inherits TrackedComponentBase`. Routes whose page component
   directly inherits `TrackedComponentBase` receive `isWatermarked: true`
   on every element entry. Non-watermarked routes receive `isWatermarked: false`.
7. Writes one manifest per role at
   `docs/component-catalog/{role}.json` and a merged
   `docs/component-catalog/index.json`.

## Catalog schema (schemaVersion 1.1)

Each element entry in `index.json` / `{role}.json`:

| Field | Type | Description |
|---|---|---|
| `routePath` | string | Normalized route template (e.g. `/admin/products`, `/customers/{CustomerId}`) |
| `role` | string | Role under which this element was crawled (e.g. `SystemAdmin`) |
| `elementRole` | string | W0 element-role enum value |
| `userVisibleLabel` | string | Accessible name visible to the user |
| `ariaLabel` | string? | `aria-label` attribute if present |
| `testId` | string? | `data-testid` attribute if present |
| `htmlTag` | string? | HTML tag (lowercase) |
| `scope` | string? | Nearest dialog/region label, if any |
| `isWatermarked` | bool | `true` iff the page component for this route directly inherits `TrackedComponentBase`. When `true`, UAT checklist steps targeting this route should emit `WAIT FOR ACTIONS QUIET` after each interaction verb. |

**schemaVersion history:**
- `1.0` — initial schema
- `1.1` — added `isWatermarked` field (`uat-dsl-watermark-integration` Session 1)

## Why DOM-walk and not `IAccessibility.SnapshotAsync`?

Playwright's accessibility-tree snapshot is marked obsolete in 1.53+ and
additionally trips the Playwright stdio transport's
`System.Text.Json` 64-deep parse limit on richly nested MudBlazor screens
(e.g. the security dashboard tabs cause a `MaxDepth` exception). The
in-page DOM walk returns a flat array, avoids the depth limit, and
captures the same role + accessible-name fields the DSL validator
locates by.

## CI integration

Catalog re-crawl is on demand (e.g., before authoring a new UAT
checklist), not in CI — booting the full app on every push is not
warranted yet. CI integration is documented but not enforced by W1. W2
(`naive-uat-runner-with-feedback`) and W3
(`playwright-sync-watermark-and-playbook`) will gate validation in CI.

## Options

| Flag | Default | Description |
|---|---|---|
| `--output <dir>` | `docs/component-catalog` | Output directory |
| `--base-url <url>` | `http://localhost:5010` | Web base URL |
| `--api-base-url <url>` | `http://localhost:5011` | API base URL |
| `--test-auth-secret <s>` | `acme-local-seeded-session` | Seeded-session shared secret |
| `--internal-api-key <k>` | `dev-internal-key-acme` | Internal API key |
| `--pages-dir <path>` | `src/Apps/AcmeCoffeeOnline.Web/Components/Pages` | Razor pages dir |
| `--seed-email <email>` | `admin@acmecoffee.com` | Email to authenticate with |
| `--roles <r1,r2,...>` | all 5 | Comma-separated roles |
| `--headed` | (headless) | Run Chromium visibly |
