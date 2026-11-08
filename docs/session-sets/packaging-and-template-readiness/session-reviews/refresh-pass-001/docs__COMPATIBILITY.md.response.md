# Refresh response for `docs/COMPATIBILITY.md`

- Model: gemini-pro
- Input tokens: 2418
- Output tokens: 1494
- Cost: $0.0180

---

```markdown
# Version Compatibility — W0 Toolchain × Catalog × W2 Runner

> Authoritative compatibility matrix for the W0 (`dabbler-uat-dsl`)
> packages, the component-catalog schema, the top-level W2 runner (`uat_runner`),
> and the integrated watermark library. `Directory.Packages.props`
> bumps for any of these reference this matrix; `dabbler-uat-dsl`'s
> README links to it as the integrator-side source of truth.

The W0 toolchain is split across three NuGet packages
(`Dabbler.Uat.Dsl.Core`, `Dabbler.Uat.Dsl.Cli`,
`Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`) plus a JSON
catalog-schema contract and a Python runner that wraps
`dotnet test`. This document fixes the supported combinations.

The DSL-language reference, CLI flag reference, and data-contract
schemas live in the sibling repo:

- [`dabbler-uat-dsl/docs/language-reference.md`](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/language-reference.md)
- [`dabbler-uat-dsl/docs/cli-reference.md`](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/cli-reference.md)
- [`dabbler-uat-dsl/docs/data-contracts.md`](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/data-contracts.md)

This file does **not** restate that material; it only pins the
versions.

---

## Current supported combination

| Component                                          | Version / State | Source |
|----------------------------------------------------|-----------------|--------|
| `Dabbler.Uat.Dsl.Core`                             | 1.2.0           | `Directory.Packages.props` |
| `Dabbler.Uat.Dsl.Cli`                              | 1.2.1           | `Directory.Packages.props` |
| `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`       | 1.1.0           | `Directory.Packages.props` |
| Component-catalog schema                           | 1.1             | `tools/ComponentCatalogCrawler/` |
| W2 runner (`uat_runner`)                           | Tracks W0 1.2.x flags (`--suppressions`, `--active-role`) | Top-level `uat_runner/` package |
| `Dabbler.Testing.Blazor` (watermark library)       | Integrated source | `src/Libs/Dabbler.Testing.Blazor/` |

All components must align: a W0 Core version that does not understand
the `--active-role` flag (requires CLI ≥ 1.2.1) cannot consume a checklist
authored under the v1.1 catalog's per-role grounding without
cascading `UnknownRoute` errors after the first sign-in step.

---

## Compatibility matrix

| W0 Core | W0 Cli | W0 Emitters | Catalog schema | W2 runner expects |
|---------|--------|-------------|----------------|-------------------|
| 1.0.0   | (none) | 1.0.0       | 1.0            | not yet shipped — W0 invoked directly |
| 1.1.0   | 1.2.0  | 1.1.0       | 1.0 or 1.1     | `--suppressions` plumbing (compile honors it natively) |
| **1.2.0** | **1.2.1** | **1.1.0** | **1.1**        | `--suppressions` + `--active-role`; auto-infers active role from persona |

Bold rows are the **currently supported** combinations. Bumping any
single component out of band requires re-pinning the others to a known-good
row from this table; ad-hoc combinations are unsupported.

### Schema upgrade notes

- **1.0 → 1.1.** Adds the optional `isWatermarked` boolean per entry.
  v1.0 catalogs continue to validate; W0 ignores the field. Consumers
  that author checklists for watermarked routes need v1.1 catalogs to
  decide where to emit `WAIT FOR ACTIONS QUIET`.
- The W0 `JsonCatalogAdapter` accepts unknown fields silently, so
  consumers can carry diagnostic data beyond the published schema
  without breaking validation.

### Flag expectations

The W2 runner (`uat_runner`, invoked via `python -m uat_runner.cli`)
calls `dabbler-uat-dsl compile` with both `--suppressions
tools/uat-suppressions.json` and `--active-role <inferred>` on every
invocation. Both flags require W0 Cli ≥ 1.2.1.

---

## Bump procedure

When updating any pinned version:

1. Repack the sibling DSL repo
   (`dotnet pack ../dabbler-uat-dsl/DabblerUatDsl.sln -o ../dabbler-uat-dsl/pack-output`)
   if the bump is to a Core / Cli / Emitter package.
2. Update `Directory.Packages.props` with the new `PackageVersion`s.
3. Update the **Current supported combination** table in this file
   and add a new row to the **Compatibility matrix** if the
   combination is novel.
4. Re-run the W2 watermark smoke
   (`docs/session-sets/w0-cli-suppressions-and-role-pinning/smoke/run-5x-smoke.py`)
   for a 5/5 pass before committing.
5. Note the bump in the consuming session set's `change-log.md`.

When updating the catalog schema:

1. Bump `tools/ComponentCatalogCrawler/`'s output `schemaVersion` literal.
2. If the schema change is breaking, bump W0 Core's major version
   (so consumers get a NuGet-resolution failure rather than a runtime
   surprise).
3. Re-crawl: `tools/ComponentCatalogCrawler/` writes
   `docs/component-catalog/index.json` against the running compose stack.
4. Update [`dabbler-uat-dsl/docs/data-contracts.md`](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/data-contracts.md)
   in the sibling repo to describe the new schema version, and bump
   the schema row in the table above.
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

The refresh is mostly good on the path updates, but it contains a real technical mistake:

- It conflates **Core** with the **CLI** for flag support.  
  The paragraph beginning “All components must align…” says a “W0 Core version” does not understand `--active-role`, and the flag expectations section says **both** `--suppressions` and `--active-role` require W0 CLI ≥ 1.2.1. That is not right: flags are a CLI concern, and the matrix itself implies `--suppressions` was already supported in the 1.2.0 CLI row. Only `--active-role` should be tied to 1.2.1.

No other obvious stale `ai-router/uat_runner/` references remain; those were correctly updated to top-level `uat_runner/`.
