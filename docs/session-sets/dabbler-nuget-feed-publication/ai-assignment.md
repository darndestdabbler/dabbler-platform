# AI Assignment — dabbler-nuget-feed-publication

- **Tier:** full · **Requires UAT:** false · **Requires E2E:** false
- Per-session recommendations below are the output of routed analysis
  (`route(task_type="analysis")` → Gemini 2.5 Pro, $0.0155, Session 1 author time),
  independent of the orchestrating model per the "never self-opine on cheaper model" rule.

## Session 1: Feed approach + versioning decision (architecture)

### Recommended orchestrator (engine + model + effort)
Claude Code / `claude-opus-4-8` / high

### Rationale
This session addresses a hard-to-reverse, public-facing distribution decision. The `architecture`
task type correctly mandates a high-capability orchestrator and a tri-provider review to ensure a
robust, forward-looking strategy.

### Estimated routed cost
~$0.22 (moderate)

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1.1 | Lock the feed crux (public/private, audience, infra) with the owner. | `AskUserQuestion` (human-only business facts) |
| 1.2 | Extract package surface, dependency graph, license/transitive facts. | direct work |
| 1.3 | Author the orchestrator's panel contribution. | `orchestrator` (Opus direct, full context) |
| 1.4 | Tri-provider panel review (self-contained prompt). | `call_model(gpt-5-4)` + `call_model(gemini-pro)` |
| 1.5 | Synthesize reviews and record the decision. | direct work |
| 1.6 | Author `ai-assignment.md` recommendation. | `route(task_type="analysis")` |
| 1.7 | Mandatory cross-provider session verification. | `route(task_type="session-verification")` (cross-provider) |

### Actuals (filled after the session)
- **Orchestrator used:** Claude Code / `claude-opus-4-8` / high
- **Total routed cost:** GPT-5.4 architecture review $0.1814 + Gemini 2.5 Pro architecture review
  $0.0281 + Gemini 2.5 Pro ai-assignment analysis $0.0155 + cross-provider session-verification
  (see `disposition.json` for the final figure).
- **Deviations from recommendation:** None — recommendation matched (the `architecture` task type
  forces Opus in `router-config.yaml`, and the orchestrator was already Opus, so it contributed its
  panel seat directly per the Tri-Provider full-context convention rather than routing to itself).
- **Notes for next-session calibration:** The load-bearing cost was the GPT-5.4 panel review at
  `reasoning_effort=high` (~242s, $0.18); Gemini Pro was an order of magnitude cheaper. The one
  divergence (GPT-5.4 wanted to exclude the `Platform.Stubs*` packages) was resolved by local
  ground truth (the template hard-depends on `Dabbler.Platform.Stubs`), not by a further routed
  round — verifying the dependency graph locally is cheaper than another panel pass.

**Next-session orchestrator recommendation (Session 2):** GPT-5.4 Code / `gpt-5-4-mini` / medium
Rationale: Session 2 is standard CI/CD engineering (GitHub Actions YAML + PowerShell pack-script
parameterization); a capable tier-2 model handles structured config/script generation cost-effectively.

## Session 2: Publish pipeline

### Recommended orchestrator (engine + model + effort)
GPT-5.4 Code / `gpt-5-4-mini` / medium

### Rationale
This session involves standard CI/CD engineering (YAML, PowerShell) and careful one-time publish
guardrails. A capable, cost-effective tier-2 model is ideal for generating and refining structured
configuration and script files.

### Estimated routed cost
~$0.10 (low)

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 2.1 | Confirm owner prerequisites exist (nuget.org account, scoped API key, GitHub Environment secret). | `human_action` (gate before pipeline work) |
| 2.2 | Parameterize `tools/pack.ps1` to stamp the tag version and pack the public allowlist. | direct work |
| 2.3 | Draft `nuget-publish.yml`: trigger on `nuget-v*` tag, parse version, pack, build-once/promote, push with `--skip-duplicate`. | direct work |
| 2.4 | Add protected Environment + required reviewers, scoped API-key secret, `.snupkg` symbols, deterministic build + Source Link. | direct work |
| 2.5 | Pre-first-push guardrails: GH-Packages dry-run, package-validation, license/metadata completeness (incl. DabblerPdf copyleft notice). | direct work |
| 2.6 | Owner pushes tag `nuget-v0.1.0-preview.1` and approves the first run; verify restorability. | `human_action` |
| 2.7 | Mandatory cross-provider session verification. | `route(task_type="session-verification")` (cross-provider) |

### Actuals (filled after the session)
- Orchestrator used:
- Total routed cost:
- Deviations from recommendation:
- Notes for next-session calibration:

**Next-session orchestrator recommendation (Session 3):** Claude Code / `claude-sonnet-4-6` / medium
Rationale: Session 3 is straightforward template edits + docs + a consumer-validation scaffold and
closeout; a cost-effective tier-2 model is fully capable without tier-3 expense.

## Session 3: Consumer wiring + remove vendoring hack

### Recommended orchestrator (engine + model + effort)
Claude Code / `claude-sonnet-4-6` / medium

### Rationale
This session combines straightforward file edits, documentation updates, and project analysis, plus
the final closeout + Step-9 reorganization review. A cost-effective tier-2 model handles these
without the expense of a tier-3 orchestrator.

### Estimated routed cost
~$0.08 (low)

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 3.1 | Update template `nuget.config` to feed-only (`<clear/>` + nuget.org); remove local `./packages` default + `packages/.gitkeep`. | direct work |
| 3.2 | Update packaging/consumer docs + template README for feed-based restore and version pin/upgrade. | direct work |
| 3.3 | Consumer-validation gate: clean-checkout scaffold, restore from the feed, build + test green. | direct work → `human_action` if a clean machine is required |
| 3.4 | Closeout: `change-log.md` (end-of-set only), `disposition.json`, note downstream NorthwindToBlazor vendoring cleanup. | direct work + `route(task_type="summarization")` for change-log prose |
| 3.5 | Step-9 reorganization review: `project-guidance.md` + `lessons-learned.md` promotion candidates. | `route(task_type="analysis")` |
| 3.6 | Mandatory cross-provider session verification. | `route(task_type="session-verification")` (cross-provider) |

### Actuals (filled after the session)
- Orchestrator used:
- Total routed cost:
- Deviations from recommendation:
- Notes for next-session calibration:
