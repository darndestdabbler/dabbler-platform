# Notes-Driven Template Encoding Spec

> **Purpose:** Encode only the simplified, proven platform surface into
> `template-seed/` and related bootstrap guidance so new repos inherit the
> narrowed contract instead of the broader historical one.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-template-encoding/`
> **Prerequisite:** `notes-driven-app-alignment` is complete and
> `docs/planning/notes-driven-app-acceptance-baseline.md` is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set is intentionally last.

It should only encode what survived the audit, simplification, and app-alignment
work. Templates must not become the hiding place for advanced functionality that
the narrowed contract no longer wants to lead with.

The template should explicitly avoid reintroducing retired public surfaces or
isolated advanced defaults just because they once existed in the broader
library story.

Template work should inherit only surfaces that survived the audit and were
already protected by the simplification coverage gate and accepted app
baseline.

The current accepted app baseline is specific about a few defaults that the
template should encode carefully:

- start from the focused shell/dashboard composition now proven in the app:
  no catalog or usage-tracking provider wiring in the default shell, no
  `DabblerSecurityNav`, and no favorites/recent-items dashboard defaults
- keep the focused notes-driven query/report workflows front and center:
  query-page CSV export is a deliberate app-level opt-in, while report pages
  keep the accepted PDF-export baseline without reintroducing print/zoom
  controls
- treat the `/products/catalog` route pattern as a focused alias choice rather
  than evidence that starter templates should bring back a broad catalog
  browsing surface
- do not treat `DabblerMultiComboBox<T>` as part of the minimal starter
  surface unless a concrete scaffolded workflow actually needs multiselect
  lookup behavior

Treat `docs/planning/notes-driven-library-compatibility-notes.md` and
`docs/planning/notes-driven-app-acceptance-baseline.md` as negative-default
lists for scaffolding: compatibility-only catalog/query-report surfaces,
library-default admin/navigation assumptions, and intentionally isolated
advanced grid/filter/dashboard extras should not reappear in `template-seed/`
as starter defaults.

## Session Plan

### Session 1 of 3: Template Contract Audit

**Steps:**
1. Compare `template-seed/` and related bootstrap docs to the accepted
   simplified platform surface.
2. Identify which defaults, placeholders, docs, and starter session-set assets
   need to change.
3. Record any template limitations that must remain explicit rather than
   silently implied.

**Creates:** Template contract audit and update list
**Touches:** Planning docs and template work notes
**Ends with:** Template changes are driven by proven platform behavior rather
than speculation
**Progress keys:** `session-1/template-audit`,
`session-1/defaults-review`, `session-1/update-list`

---

### Session 2 of 3: Template And Guidance Updates

**Steps:**
1. Update `template-seed/` assets, bootstrap docs, and starter session-set
   guidance so they expose only the retained notes-driven defaults.
2. Ensure advanced or intentionally isolated features are not scaffolded by
   default, including retired catalog/query-report surfaces, compatibility
   shell/admin wiring, and non-core grid/filter/dashboard extras. Treat query
   CSV export as a deliberate app-level choice, preserve the accepted
   PDF-export-only report baseline, and add multiselect lookup support only
   when a concrete scaffolded workflow actually needs it.
3. Update any workflow/bootstrap guidance that still implies the older broader
   library surface.

**Creates:** Updated template seed and guidance
**Touches:** `template-seed/` and related docs
**Ends with:** New repos start from the narrowed, supported platform surface by
default
**Progress keys:** `session-2/template-updates`,
`session-2/docs-updates`, `session-2/default-surface`

---

### Session 3 of 3: Scaffold Smoke Proof, Rebaseline, And Closeout

**Steps:**
1. Add or update smoke checks proving a newly scaffolded repo inherits the
   intended minimal platform surface and workflow assets, including the focused
   shell/admin baseline, dashboard defaults, query/report defaults, and the
   absence of compatibility-only catalog/query-report surfaces.
2. Revisit the remaining session-set queue and update assumptions if the
   template work exposed any new simplification lessons.
3. Append one catch-up session only if the template work exposed a direct
   omission that belongs in this set.
4. Generate `change-log.md` for this session set.

**Creates:** Template smoke-proof notes/tests and `change-log.md`
**Touches:** Template smoke assets, planning docs, and closeout artifacts
**Ends with:** The template seed repeats only the proven, simplified platform
contract
**Progress keys:** `session-3/template-smoke-proof`,
`session-3/queue-rebaseline`, `session-3/change-log`
