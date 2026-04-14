# Notes-Driven Template Encoding Spec

> **Purpose:** Encode only the simplified, proven platform surface into
> `template-seed/` and related bootstrap guidance so new repos inherit the
> narrowed contract instead of the broader historical one.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/notes-driven-template-encoding/`
> **Prerequisite:** `notes-driven-app-alignment` is complete and its acceptance
> baseline is current.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This session set is intentionally last.

It should only encode what survived the audit, simplification, and app-alignment
work. Templates must not become the hiding place for advanced functionality that
the narrowed contract no longer wants to lead with.

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
   default.
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
   intended minimal platform surface and workflow assets.
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
