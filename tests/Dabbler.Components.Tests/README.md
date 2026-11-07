# Dabbler.Components Contract Tests

This project is the dedicated contract-test home for the retained
`Dabbler.Components` surface defined in
`docs/planning/notes-driven-supported-library-surface.md`.

Session 1 establishes the project and a minimal smoke baseline. Later
simplification sessions should add direct coverage for:

- `DabblerQueryPage<T>`
- `DabblerDataGrid<T>` core behavior
- `DabblerFilterGrid<T>` core behavior
- `DabblerComboBox<T>` plus retained multiselect behavior through
  `DabblerMultiComboBox<T>`
- `DabblerDetailForm<T>`
- `DabblerRecordPopup<T>`
- `DabblerDashboardPage` core behavior

Session 4 adds direct shell/dashboard contract coverage so catalog plumbing and
usage-tracking widgets can be isolated without relying only on app smoke tests.
