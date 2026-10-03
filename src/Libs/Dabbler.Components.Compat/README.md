# Dabbler.Components.Compat

> **Status:** DEPRECATED — slated for removal in `0.4.0`.
> **Sunset version:** `0.4.0`.
> **Reason for existence:** Pre-MudBlazor-native compatibility surfaces
> carved out of `Dabbler.Components` per [ledger §3.4](../../docs/session-sets/packaging-and-template-readiness/packaging-readiness-ledger.md#34-compatibility-surfaces--moving-to-dabblercomponentscompat).

## Audience

Consumers who still have code referencing the legacy compatibility
shell, navigation, workspace, menu, or catalog page components from
`Dabbler.Components` and need a transition window before migrating to
MudBlazor-native primitives directly. **New code should not adopt this
package.** Use `MudDataGrid` + `MudLayout` + `MudDrawer` + `MudNavMenu`
from MudBlazor (composed à la carte) instead.

## Prerequisites and stack constraints

- Blazor Server with MudBlazor 8.x
- .NET 10 (current LTS)
- A reference to `Dabbler.Components` (this package overlays it; it
  does not replace it)

## Status as of Session 6 (`packaging-and-template-readiness`)

- The project scaffold (this csproj + this README) is committed.
- **The file moves for `DabblerShell`, `DabblerNavigationPanel`,
  `DabblerTabbedWorkspace`, `DabblerTopMenuBar`, and `DabblerCatalogPage`
  land in Session 7** as a mechanical move from `src/Libs/Dabbler.Components/`
  into this project, with namespace retargeting from
  `Dabbler.Components.*` to `Dabbler.Components.Compat.*`.
- A full README with install instructions, registration snippets, and
  the migration path to MudBlazor-native primitives lands in Session 7
  after the content move.

## Install (post-Session 7)

```bash
dotnet add package Dabbler.Components.Compat --prerelease
```

Until Session 7's content move ships, **do not** install this package —
the surfaces it advertises still live inside `Dabbler.Components`.

## Sunset policy

- Through `0.3.x-preview.N`: package publishes alongside
  `Dabbler.Components`. Consumers with legacy code keep working.
- At `0.4.0`: package **ceases publication**. Consumers pinned to
  `0.3.x` of `Dabbler.Components.Compat` remain on those versions
  indefinitely. New consumers cannot install at `0.4.0` or above.
- The five compat types remain in `Dabbler.Components.Compat` 0.3.x
  history forever (NuGet is append-only); they are simply not present
  in newer versions.

## Related packages

- [`Dabbler.Components`](../Dabbler.Components/README.md) — the current
  MudBlazor-native CRUD primitives. The migration target.
