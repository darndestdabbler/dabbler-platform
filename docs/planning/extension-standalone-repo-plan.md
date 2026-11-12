# Extension Standalone Repo Plan — `dabbler-session-sets-extension`

**Status:** Planning only. Repository does not yet exist.
**Trigger:** Identified during portability review — two repos
(`dabbler-platform` and `dabbler-access-harvester`) independently
maintain copies of the extension with diverging namespaces and logic.
A standalone shared repo eliminates the copy problem.

---

## Namespace decision

**`dabblerSessionSets`** — already used by the harvester (v0.7.1) and
now aligned in platform (after the rename in this session). The shared
namespace means both existing repos already share command IDs and
extension activation events. No consumer-side migration needed at
install time.

---

## Repo contents

```
dabbler-session-sets-extension/
  extension.js      — generic-namespace, portable extension logic
  package.json      — name: dabbler-session-sets, namespace: dabblerSessionSets
  media/
    icon.svg
    done.svg
    in-progress.svg
    not-started.svg
  README.md         — explains universal use, settings, per-repo gating
  CHANGELOG.md      — version history
  .vscodeignore     — exclude dev artifacts from packaged VSIX
```

No repo-specific logic is bundled. Per-repo behavior comes from:
- The spec files the extension reads (Session Set Configuration blocks)
- Workspace settings (`dabblerSessionSets.uatSupport.enabled`, etc.)

---

## Baseline and version strategy

| Milestone | Version | Notes |
|---|---|---|
| Harvester's current published copy | v0.7.1 | Starting baseline — UAT commands absent |
| Platform portability hardening | v0.8.0 | Added UAT/E2E gating, settings, configurable Playwright path |
| First standalone-repo release | v1.0.0 | Signal: officially a shared dependency |

v0.8.0 is the merge point: take platform's extension.js (which adds the UAT/E2E hardening) and reconcile against harvester's v0.7.1 if any divergence exists. Then tag v0.8.0 in the standalone repo.

---

## Per-repo UAT/E2E gating — how it works (no consumer changes needed)

The extension reads `spec.md` in each session-set directory and parses
the `## Session Set Configuration` YAML block. Two workspace context
keys (`dabblerSessionSets.uatSupportActive`, `dabblerSessionSets.e2eSupportActive`)
are set based on whether any spec in the workspace declares
`requiresUAT: true` or `requiresE2E: true`. The `when` clauses in
`package.json` gate the UAT/E2E commands on those context keys. All of
this happens automatically — consuming repos don't configure anything
for the gating to work correctly in `auto` mode.

---

## Settings (each is workspace-scoped in the consuming repo)

| Setting | Default | Purpose |
|---|---|---|
| `dabblerSessionSets.uatSupport.enabled` | `auto` | `auto`: show UAT features when any spec opts in. `always` / `never`: override. |
| `dabblerSessionSets.e2eSupport.enabled` | `auto` | Same for E2E commands. |
| `dabblerSessionSets.e2e.testDirectory` | `tests` | Root dir to search for E2E test files. Repos with a specific Playwright project set this to that directory (e.g., `tests/MyApp.Playwright`). |

---

## Hardcoded path eliminated

The original platform copy had `PLAYWRIGHT_REL` hardcoded to
`tests/AcmeCoffeeOnline.Playwright`. This was replaced with the
`dabblerSessionSets.e2e.testDirectory` setting (default `tests/`).
Platform's workspace settings should set:

```json
{
  "dabblerSessionSets.e2e.testDirectory": "tests/AcmeCoffeeOnline.Playwright"
}
```

Harvester (no E2E) sets `dabblerSessionSets.e2eSupport.enabled: never`
and ignores the path entirely.

---

## How existing repos reference the extension

**Phase 1 (current): bundle VSIX locally.** Each repo contains
`tools/vscode-session-sets/` with the extension source. Developers
build and install the VSIX from their own copy. This is what both
repos do today.

**Phase 2 (after standalone repo): install from a shared local path.**
Each consuming repo removes `tools/vscode-session-sets/` and instead
references the standalone repo. Developers install via:

```bash
# From the standalone repo (once, per developer machine)
vsce package
code --install-extension dabbler-session-sets-0.8.0.vsix
```

Alternatively, pin the VSIX file inside the standalone repo and
reference it from a shared scripts or `Makefile` target:

```bash
code --install-extension ../dabbler-session-sets-extension/dabbler-session-sets-0.8.0.vsix
```

**Phase 3 (optional): VS Code Marketplace.** If the extension is
published, `code --install-extension darndestdabbler.dabbler-session-sets`
works without the standalone-repo checkout. Enables auto-updates.
Requires a Marketplace publisher account.

---

## Migration steps for existing repos

When the standalone repo is created:

1. Remove `tools/vscode-session-sets/` from each consuming repo.
2. Add a one-line developer-setup note to each repo's README: "Install
   the Session Set Explorer extension from the standalone repo (see
   `dabbler-session-sets-extension/README.md`)."
3. Add `.vscode/settings.json` workspace settings for any repo-specific
   values (`e2e.testDirectory`, hard-disable settings for no-UI repos).
4. Optionally: add a `tools/install-extension.sh` (or PowerShell
   equivalent) that grabs the latest VSIX from the standalone repo
   and installs it.

---

## Open decision before creating the repo

The extension currently activates on `workspaceContains:docs/session-sets`.
This is generic enough for any Dabbler AI-led-workflow repo. Confirm
this is the right activation trigger before v1.0.0 release.
