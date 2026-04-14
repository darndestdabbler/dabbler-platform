# Integrated Library Home

This repo's reusable libraries now live together under `src/Libs/` so app,
library, and workflow work can ship from one solution without sibling-repo
coordination.

Current integrated library paths:

- `src/Libs/Dabbler.Components`
- `src/Libs/Dabbler.Reports.Core`
- `src/Libs/Dabbler.Reports.Export`
- `src/Libs/Dabbler.Reports.Blazor`
- `src/Libs/Dabbler.Security`
- `src/Libs/Dabbler.Security.Blazor`

The current supported reusable contract is defined in
`docs/planning/notes-driven-supported-library-surface.md`.

Dedicated library test homes now include:

- `tests/Dabbler.Components.Tests`
- `tests/Dabbler.Reports.Blazor.Tests`
- `tests/Dabbler.Security.Tests`
- `tests/Dabbler.Security.Blazor.Tests`
