# Post-Rebuild UAT — Session 1 Review

## UAT Execution Summary

The human reviewer completed the full 29-item UAT checklist covering the
AcmeCoffeeOnline application after the MudBlazor-native rebuild and E2E
workflow coverage session sets.

**Results:** 13 passed, 16 failed (including empty "Other" rows)

## Key Findings By Category

### Layout (Fixed)
- Content hidden behind fixed app bar — systemic across all pages
- Security Dashboard had duplicate/oversized headers
- Report columns used only ~63% of available landscape page width

### Functional Bugs (Fixed)
- Dashboard search fields discarded user input on navigation
- Product detail Undo button permanently disabled (5 fields missing
  ValueChanged callbacks)
- Report date pickers rejected typed dates (readonly by default)
- Admin drawer hidden for org-scoped admins (role check too narrow)

### Styling (Fixed)
- Currency and date columns left-aligned in grids
- Grids lacked column borders for visual separation

### Deferred Items
- **App bar identity redesign** — user proposed "Name as Role for Org"
  hyperlink pattern. Significant UX change, needs design session.
- **Security Dashboard empty tabs** — registration calls not wired in
  Web app. Tracked as `security-dashboard-wiring` session set.
- **Filter panel not working** — E2E tests pass; needs investigation.
- **Font size consistency** — minor, acceptable per reviewer.

## Remediation Outcome

All fixable issues were addressed in the same session. The remediation
touched 15 files across layout, styling, functional bugs, report definitions,
and lessons learned. Build clean, all 467 non-Playwright tests pass.

## Lessons Learned Added

Five new entries added to `docs/planning/lessons-learned.md`:
1. UAT checklists must include explicit test data and search terms
2. Navigation actions must pass user input to the target page
3. Fixed-position app bars need a content offset
4. ValueChanged callbacks must cover all editable fields
5. Currency and date columns should be right-aligned by default

Grid styling convention added to `docs/planning/guiding-principles.md`.

## Verdict

**SESSION COMPLETE** — All addressable UAT findings remediated. Deferred
items tracked with clear ownership. Downstream session set created for
Security Dashboard wiring.
