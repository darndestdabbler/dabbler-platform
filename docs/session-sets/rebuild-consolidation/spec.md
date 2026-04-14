# Rebuild Consolidation Spec

> **Purpose:** Rebuild AcmeCoffeeOnline and consolidate the supporting Dabbler
> libraries, AI-led workflow assets, and contributor scaffolding into a single
> integrated repo/solution with stronger guardrails, deterministic test
> infrastructure, meaningful E2E coverage, and a reusable local template seed.
> **Created:** 2026-04-14
> **Session Set:** `docs/session-sets/rebuild-consolidation/`
> **Prerequisites:** Current quality-remediation work must reach a trustworthy
> baseline with documented lessons learned and parity targets.
> **Workflow:** Implement per session, verify independently each session, and
> use the current demo repo plus sibling repos as reference inputs rather than
> long-term homes.

---

## Project Overview

This session set is not a blind rewrite.

It follows a hybrid path:

1. Stabilize the current demo only enough to make it a trustworthy reference.
2. Capture intended behavior, escaped issues, and prevention rules.
3. Rebuild the app and consolidate the required platform assets in a new
   integrated repo/solution.

The rebuild is justified because the current demo exposed foundational
integration failures that should have been prevented by templates, library
defaults, and test infrastructure:

- missing Dabbler component CSS asset links
- missing `@Body` in the main layout
- mismatched dev magic-link behavior and test assumptions
- permission tests drifting from `security-config.json`
- missing deterministic test sign-in path for non-auth tests
- sibling-repo drift between app, libraries, and orchestration assets

This session set now treats `../dabbler-platform` as the future canonical home
for:

- AcmeCoffeeOnline app code
- required Dabbler libraries and samples
- AI-led workflow tooling (`ai-router`, prompts, utilities)
- root orchestrator instructions (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`)
- selected docs, baton history, and session-set artifacts worth carrying
  forward
- a first local template seed for bootstrapping the same workflow in future
  repos

The older `library-consolidation` plan should be treated as overlapping with
this work unless the human explicitly revives it as a narrower follow-on effort.

---

## Migration Principles

- Scaffolding prevents first.
- Library defaults prevent second.
- Tests enforce third.
- Documentation and instructions reinforce fourth.
- Rebuild fragile integration seams.
- Migrate proven business logic.
- Default to Docker-first E2E infrastructure.
- Default to xUnit for all new test projects.
- Prefer one canonical repo over sibling source references.
- Keep the first template seed focused on workflow bootstrap, not full product
  scaffolding.

---

## Migration Boundaries

### Migrate with minimal redesign

- domain entities and DTOs
- API contracts
- business logic and data-access behavior that is already correct
- validated report logic and business rules
- validated role definitions and authorization intent
- known-good user journeys and parity expectations
- AI router implementation and prompt assets that already support the
  session-set workflow
- active architecture/testing docs that still help humans and agents work from
  the integrated repo

### Reimplement deliberately

- solution structure and project wiring
- startup/DI composition
- shell and layout composition
- static asset inclusion strategy
- auth and test infrastructure
- templates and scaffolding
- Playwright infrastructure and conventions
- setup and contributor documentation
- final integrated location and pathing of AI workflow assets
- local template-seed structure and placeholder strategy

### Do not carry forward automatically

- brittle test helpers tied to incorrect environment assumptions
- manual permission bridging that should become library-driven
- one-off workarounds that encode the current repo's accidental structure
- duplicate `ai-router` or agent-instruction copies from multiple sibling repos
- archival docs and session histories that add noise without helping current
  work

---

## Session Plan

### Session 1 of 6: Baseline Capture And Lessons Learned

**Steps:**
1. Inventory the current demo's intended behavior and confirmed working flows.
2. Document escaped issues, root causes, and the prevention layer that should
   own each one: scaffolding, library, tests, or documentation/process.
3. Create a parity checklist for the rebuilt app.
4. Create a "do not carry forward" list of fragile patterns from the current
   repo.
5. Define the minimum critical user journeys the rebuilt app must match before
   cutover.

**Creates:** Baseline capture, lessons learned, parity checklist, and
carry-forward guidance docs
**Touches:** New docs under this session set
**Ends with:** Rebuild work has a clear source of truth and agreed success
target
**Progress keys:** `session-1/baseline-capture`, `session-1/lessons-learned`,
`session-1/parity-checklist`, `session-1/do-not-carry-forward`

### Session 2 of 6: New Repo Foundation And Docker-First Test Harness

**NOTE:** This session will operate in the new integrated repo, not primarily
in `dabbler-demo`.

**Steps:**
1. Create the new integrated repo and solution skeleton.
2. Add central build/package management and the initial project structure for
   app, libraries, and tests.
3. Add Docker Compose for the web app, API, database, email test service, and
   Playwright runner.
4. Create xUnit-based test projects and the initial Playwright test project.
5. Prove the new repo can boot and run a smoke E2E test in local and CI-like
   conditions.

**Creates:** New repo skeleton, solution, Docker files, and test harness
foundation
**Touches:** New integrated repo files
**Ends with:** `docker compose up` can bring up the initial stack and a smoke
Playwright test passes
**Progress keys:** `session-2/repo-skeleton`, `session-2/docker-stack`,
`session-2/xunit-foundation`, `session-2/playwright-smoke`

### Session 3 of 6: Library Consolidation And Build Unification

**NOTE:** This session operates in the new integrated repo.

**Steps:**
1. Move the Dabbler security projects into the integrated repo under the new
   package boundaries.
2. Align shared package versions and build settings.
3. Remove sibling-repo assumptions and convert to integrated source
   references.
4. Ensure the consolidated libraries build and test successfully from the new
   repo.
5. Document namespace/package transitions and migration notes.

**Creates:** Consolidated library structure and migration notes
**Touches:** Dabbler library projects, build files, solution files
**Ends with:** One integrated repo can build the app-facing libraries in a
single pass
**Progress keys:** `session-3/security-move`,
`session-3/build-unification`, `session-3/package-boundaries`,
`session-3/library-build`

### Session 4 of 6: Workflow Asset Consolidation And Platform Guardrails

**NOTE:** This session operates in the integrated repo and expands the repo
boundary beyond app/library code to include the AI-led workflow assets needed
to develop and verify work from one place.

**Steps:**
1. Copy the canonical AI-led workflow assets from `../dabbler-security` into
   the integrated repo: `ai-router/`, root orchestrator instruction files, and
   the core workflow docs needed to run future sessions from
   `../dabbler-platform`.
2. Reorganize the integrated repo so `prompt-templates/` and `utils/` live
   under `ai-router/` for manageability while `AGENTS.md`, `CLAUDE.md`, and
   `GEMINI.md` remain at repo root.
3. Remove old path assumptions in the router and instruction docs so prompt,
   utility, and session-log loading works correctly from the integrated repo.
4. Implement a single supported Dabbler static-asset inclusion mechanism in the
   rebuilt app foundation.
5. Add Development/Test-only deterministic auth helpers, including seeded
   session support for non-auth tests, and make auth delivery modes explicit and
   correctly named.
6. Add validation and smoke tests that specifically catch missing routed
   content, missing static assets, broken anonymous auth pages, and broken
   workflow-asset pathing.

**Creates:** Integrated AI workflow asset structure, root agent docs, startup
guardrails, auth helpers, config validation, and smoke tests
**Touches:** `ai-router/`, root instruction files, selected docs, app
templates, library integration points, and shared test infrastructure
**Ends with:** The integrated repo can run the AI-led session workflow locally,
and a freshly scaffolded app foundation cannot reproduce the missing-asset or
missing-`@Body` failures
**Progress keys:** `session-4/workflow-assets`,
`session-4/path-fixups`, `session-4/asset-guardrails`,
`session-4/auth-helpers`, `session-4/security-config-validation`,
`session-4/smoke-tests`

### Session 5 of 6: Remaining Consolidation And App Rebuild Parity

**NOTE:** This session operates in the integrated repo.

**Steps:**
1. Move or copy the remaining Dabbler component/report projects that the rebuilt
   app actually needs into the integrated repo under `src/Libs`, keeping the
   write scope focused on required packages rather than every historical demo.
2. Align those imported projects with the integrated repo's package versions,
   build settings, and source-reference layout.
3. Rebuild the AcmeCoffeeOnline app structure on the new foundation using only
   integrated-repo libraries and assets.
4. Migrate proven business logic, API behavior, auth pages, shell flows, and
   permission-aware navigation from the validated reference repos.
5. Reimplement critical demo flows feature-by-feature using the parity checklist
   from Session 1, while copying only the docs and session history that the
   integrated repo should actively own.
6. Eliminate leftover sibling-repo assumptions and legacy workaround code unless
   explicitly justified and documented.

**Creates:** Additional consolidated library structure, rebuilt app slices, and
parity-tracking updates
**Touches:** App projects, required Dabbler library projects, docs, shared
DTOs/contracts, and supporting tests
**Ends with:** The rebuilt app reaches agreed parity on the critical user
journeys and no longer depends on sibling repos for required code or workflow
assets
**Progress keys:** `session-5/components-move`,
`session-5/build-unification`, `session-5/app-rebuild`,
`session-5/domain-migration`, `session-5/auth-and-nav`,
`session-5/parity-check`

### Session 6 of 6: Meaningful E2E, Template Seed, And Cutover

**NOTE:** This session operates in the integrated repo and closes out the
migration.

**Steps:**
1. Add or finish meaningful E2E coverage for the rebuilt app.
2. Ensure the suite fails on browser console errors, Blazor runtime errors,
   page exceptions, and relevant failed requests.
3. Verify at least one real magic-link flow, deterministic seeded-session
   flows, representative permission enforcement, and at least one backend-
   verified mutating business flow.
4. Create a first local `template-seed/` or equivalent bootstrap folder that
   contains the AI-led workflow assets, root instruction files, a session-set
   skeleton, and lightweight placeholder replacement scripts.
5. Produce cutover notes and archival guidance for `dabbler-demo`,
   `dabbler-security`, and `dabbler-components`, including which repo is now
   canonical and what remains intentionally archived.
6. Distill the rebuild's durable lessons into stable integrated-repo guidance:
   create or update lesson-learned docs, guardrail docs, and any root
   instruction-file rules that future sessions should inherit by default.
7. Create a small set of ready-to-run follow-on session sets in
   `docs/session-sets/` for the next near-certain platform work, and create
   supporting planning/input documents for lower-confidence follow-on work that
   is not yet ready for a full session spec.
8. Generate `change-log.md` for this session set.

**Creates:** Final E2E coverage, local template seed, cutover notes,
lesson-learned docs, guardrail docs, initial post-rebuild session-set specs,
planning inputs for future specs, and `change-log.md`
**Touches:** E2E tests, docs, template assets, root instruction files, and
migration notes
**Ends with:** The integrated repo is ready to become the active development
home, includes a reusable local seed for repeating the AI-led workflow in
future repos, and has the first post-rebuild session sets or spec inputs ready
for the next phase of platform work
**Progress keys:** `session-6/meaningful-e2e`,
`session-6/auth-flow-verification`, `session-6/backend-verification`,
`session-6/template-seed`, `session-6/cutover`,
`session-6/lessons-and-guardrails`, `session-6/next-session-sets`,
`session-6/change-log`

---

## Required Guardrails

### Scaffolding

- app templates must wire Dabbler static assets correctly from day one
- layout templates must render routed content by default
- auth pages must use an auth-specific layout, not the main shell layout
- new test projects must default to xUnit
- Playwright templates must include browser/runtime failure detection and
  deterministic auth helpers
- root instruction files must point to paths that exist inside the integrated
  repo

### Library Behavior

- library asset wiring should prefer one supported entrypoint over manual link
  sprawl
- Development/Test startup should warn or fail fast when required Dabbler
  assets are missing
- auth delivery modes must be explicit and correctly named
- a safe Development/Test-only seeded sign-in mechanism must exist for
  automated tests
- shell/navigation permission integration should be built-in where practical,
  reducing manual app glue
- imported projects must build from integrated source references rather than
  sibling repos

### Workflow Assets

- `ai-router/` remains tooling at repo root, not under `src/`
- `prompt-templates/` and `utils/` may be nested under `ai-router/`, but their
  runtime lookup paths must be verified
- `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` remain at repo root
- duplicate router/instruction copies from sibling repos must not remain the
  implied source of truth once the integrated copy exists
- archival docs may be retained, but active guidance must clearly identify the
  integrated repo as canonical

### Tests

- smoke tests must verify routed page content is visible inside the layout
- smoke tests must verify required Dabbler CSS assets are loaded
- auth smoke tests must prove anonymous auth pages render correctly
- representative tests must validate authorization behavior against
  `security-config.json`
- meaningful E2E on mutating flows must include backend confirmation, not just
  UI clicks
- workflow-asset smoke checks must fail if prompt/template pathing is broken

### Documentation And Process

- maintain a lessons-learned mapping from escaped issue to prevention layer
- document supported dev/test auth modes clearly
- document the approved way to add pages, assets, auth flows, Playwright tests,
  and AI-workflow files
- require new features to declare the guardrails and tests that protect them
- keep the first template seed intentionally small and workflow-focused
- end the rebuild with both executable next session sets and lighter-weight
  planning inputs for future work that is not yet ready for a full spec

---

## Security And Test Infrastructure Rules

- any test-only sign-in endpoint must exist only in Development/Test
- it must be restricted to loopback or equivalent local-only access
- it must require a shared test secret or equivalent guard
- it must never be enabled in production configuration
- MailHog is the default email-capture mechanism for Docker-based E2E
- log-only or in-memory sinks are acceptable for fast local dev paths when
  clearly documented
- Twilio and SendGrid may be used for debugging or optional adapters, but are
  not baseline dependencies
- router credentials remain environment-based and must not be hard-coded into
  repo files

---

## Success Criteria

This session set is successful when:

- the integrated repo becomes the single active development home for the app,
  required libraries, and AI-led workflow assets
- the rebuilt templates make the escaped demo failures hard to reproduce
- auth and permission tests are deterministic and fast
- the shell, auth pages, and critical business flows are covered by meaningful
  E2E tests
- library/app integration no longer depends on tribal knowledge or sibling-repo
  coordination
- the integrated repo contains the root instruction files, router assets, and
  core docs needed to continue session-based work without falling back to
  sibling repos
- a first local template seed exists for bootstrapping the same workflow in a
  future repo
- the integrated repo contains durable lessons-learned and guardrail docs that
  future session sets can treat as input material
- the last rebuild session leaves behind either ready-to-run next session sets
  or structured planning inputs for any remaining post-rebuild platform work
