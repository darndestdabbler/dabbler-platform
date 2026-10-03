# AGENTS.md -- Dabbler Platform (Codex)

## Project Overview

Integrated repo for the AcmeCoffeeOnline rebuild, required Dabbler libraries,
and the AI-led workflow assets used to keep app, library, and test work in
one canonical place.

Current intended development mode: use integrated source references under
`src/Libs/`. Do not add new sibling-repo dependencies unless a session spec
explicitly requires an import from a sibling repo.

## Required reading before every session

- `README.md` -- repo layout and build entry points
- `docs/planning/project-guidance.md` -- durable principles + conventions
- `docs/planning/lessons-learned.md` -- repo-specific failure patterns and
  tactics learned from prior sessions
- [`session-set-authoring-guide.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/planning/session-set-authoring-guide.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- how to author or update a
  session-set spec, including the Session Set Configuration block
  (`requiresUAT` / `requiresE2E` / `uatScope` / `tier` / `prerequisites`),
  When-UAT-Is-Required and When-E2E-Is-Required heuristics, slug naming,
  sizing, deliverables, and anti-patterns. Pair with the local
  `docs/planning/session-set-authoring-guide.platform-addendum.md` for the
  dabbler-platform-specific extensions (Blazor / MudBlazor / Playwright /
  AcmeCoffeeOnline conventions).
- [`ai-led-session-workflow.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/ai-led-session-workflow.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- the authoritative
  session-set workflow. This is the single source of truth for the
  step-by-step procedure, the AI router usage, cross-provider verification,
  UAT checklist handling (now conditional on the spec's `requiresUAT` flag),
  **the Step 7 verifier-disagreement resolution procedure (four human
  options, disagreement logging)**, the Step 9 reorganization review (run
  after the Step 8 notify so the human is not blocking the completion
  signal), **the Delegation Discipline rules (when to route vs. handle
  directly)**, and the mandatory rules list. Do not duplicate that content
  here.
- [`greenfield-matrix-protocol.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/greenfield-matrix-protocol.md)
  (canonical, lives in `dabbler-ai-orchestration`) -- **this repo is the LEAD
  signal in the Set 075 greenfield finding-power pilot.** On
  **source-code-bearing build sessions** (skip doc / planning / mechanical
  ones), run the provider×surface verification matrix at **Step 6, BEFORE
  remediation**, following the standard instruction addendum
  [`greenfield-matrix-addendum.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/ai_router/prompt-templates/greenfield-matrix-addendum.md);
  adjudicate every finding against the fixed
  [`greenfield-adjudication-rubric.md`](https://github.com/darndestdabbler/dabbler-ai-orchestration/blob/master/docs/greenfield-adjudication-rubric.md);
  freeze the run and ship the `matrix-run/` telemetry back to the canonical
  set dir (`telemetry/dabbler-platform/<session>/`). Requires
  `dabbler-ai-router >= 0.26.0` (see `requirements.txt`). The matrix varies
  *provider*, not framing (both arms stay strong adversarial -- L-069-2).

The human starts a session with one of the trigger phrases listed in
`docs/ai-led-session-workflow.md` -> **Trigger Phrases** (sequential,
named-set, parallel-worktree, optional ` -- maxout <engine>` suffix).
That section is the canonical list; do not re-list trigger phrases
here.

What follows below is only the Codex-specific bootstrap the workflow needs.

## Codex-specific bootstrap

### API key export (Windows)

```bash
export GEMINI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('GEMINI_API_KEY', 'User')" | tr -d '\r')
export ANTHROPIC_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('ANTHROPIC_API_KEY', 'User')" | tr -d '\r')
export OPENAI_API_KEY=$(powershell -Command "[System.Environment]::GetEnvironmentVariable('OPENAI_API_KEY', 'User')" | tr -d '\r')
.venv/Scripts/python.exe -c "
import os
missing = [k for k in ('ANTHROPIC_API_KEY', 'GEMINI_API_KEY', 'OPENAI_API_KEY') if not os.environ.get(k)]
if missing:
    raise SystemExit(f'Missing environment variables: {missing}')
print('API keys OK')
"
```

If keys are missing, stop and tell the human.

### AI router import

The router is the `dabbler-ai-router` PyPI package (canonical
dabbler-ai-orchestration repo, Set 010 onwards). It is declared in
`requirements.txt` and lives in the workspace `.venv`. Bare imports
work:

```python
from ai_router import route, send_session_complete_notification
from ai_router.session_log import SessionLog
```

The platform-specific UAT runner that used to live at
`ai-router/uat_runner/` is now a top-level `uat_runner/` package
alongside the pip-installed canonical router:

```python
from uat_runner.runner import run_checklist
from uat_runner.triage import FailureRecord, TriageSession
```

The operator-tuned config lives at `ai_router/router-config.yaml`
(workspace-relative, not inside the venv). Set the env var so
`route()` reads it:

```bash
export AI_ROUTER_CONFIG=ai_router/router-config.yaml
export AI_ROUTER_METRICS_PATH=ai_router/router-metrics.jsonl
```

Use `.venv/Scripts/python.exe` to run Python scripts on Windows.
`send_session_complete_notification()` reads `PUSHOVER_API_KEY` and
`PUSHOVER_USER_KEY` from the environment or Windows User environment.

If `pip install` of the router needs to be re-run (fresh clone, dropped
venv, etc.), the canonical extension's **Dabbler: Install ai-router**
command does it from VS Code: PyPI install (default) or GitHub
sparse-checkout fallback for offline / pre-release / fork scenarios.

### Delegation Discipline (pointer)

Your role is orchestrator, not solo coder. Reasoning tasks -- code review,
security review, architecture, analysis, documentation, test generation,
session verification -- **always** go through `route()`. Only handle work
directly when it is mechanical, single-file, and under ~50 lines.

This is not cost paranoia; it is the design of the workflow. The router
already picks the cheapest capable model and applies the correct reasoning
depth from `router-config.yaml`. Your job is to pass the right `task_type`
and let the router handle the rest.

See `docs/ai-led-session-workflow.md` -> **Delegation Discipline** for the
full criteria, the human-tunable thresholds (in `ai_router/router-config.yaml`
under `delegation:`), and the rationale.

## Building & Testing

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln

# E2E (Playwright runs from the host against the app containers):
docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog (start once, leave up)
docker compose -f docker/docker-compose.app.yml   up --build -d # api + web (rebuild after code changes)
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

The compose stack is split: `docker-compose.infra.yml` holds the persistent
dev dependencies (sqlserver, mailhog, the `dabbler-dev` network) and stays
up across sessions. `docker-compose.app.yml` holds the rebuildable app
services (api, web) with explicit `image:` tags so rebuilds replace tagged
images instead of orphaning dangling layers. Playwright is intentionally
**not** a compose service -- run it from the host via `dotnet test` against
`http://localhost:5010`.

## Solution Structure

- `src/Apps/AcmeCoffeeOnline.Web/` -- Blazor Server web app
- `src/Apps/AcmeCoffeeOnline.Api/` -- ASP.NET Core Web API
- `src/Libs/` -- integrated Dabbler libraries
- `tests/` -- xUnit, bUnit, and Playwright tests
- `docker/` -- local and CI-like compose stack
- `templates/Dabbler.Templates/` -- `dotnet new` template package (`dabbler-app`,
  `dabbler-min`). The `dabbler-app` template ships a `docker/` subfolder with a
  split compose stack (SQL Server + Mailpit infra + Web + Api app services),
  per-project Dockerfiles, `appsettings.Docker.json`, and a `packages/` local
  NuGet feed for Dabbler.* packages. Consumers copy `pack-output/*.nupkg` into
  `packages/` and run two `docker compose` commands to bring the full stack up.
- `ai_router/` -- operator-tuned `router-config.yaml` and the
  append-only `router-metrics.jsonl` cost telemetry; the router
  itself is `pip install dabbler-ai-router` (declared in
  `requirements.txt`)
- `uat_runner/` -- platform-specific UAT runner package (CLI +
  triage + remediations + test_cases). Calls into the canonical
  router via `import ai_router`.
- `docs/` -- workflow docs and session sets

Code style conventions (namespaces, casing, nullable, `Async` suffix) live in
`docs/planning/project-guidance.md` -> Conventions -> Code Style.

If a file-system operation (typically `mv`, `rm -rf`, or `git worktree
remove`) fails with a Windows file-lock error, run
`.venv/Scripts/python.exe -m ai_router.utils cleanup-dev-orphans`
first -- it shuts down dotnet build servers, kills stale Claude Code
background polling loops, and clears orphan `conhost.exe`. Pass
`--match-path dabbler-platform` to scope the polling-loop kill to this
repo only. Each category is also runnable on its own
(`kill-dotnet-build-servers`, `kill-stale-claude-polls`,
`kill-conhost`). Source lives in the canonical
`dabbler-ai-orchestration` repo at `ai_router/utils.py` Section 5;
the same code is what gets pip-installed into `.venv`.

<!-- dabbler:managed:start -->
# AI orchestrator instructions — `dabbler-platform`

> `AGENTS.md` is the single source of this managed body; `CLAUDE.md`
> imports it and adds only its engine tail. Do not hand-edit inside the
> fence; re-run `dabbler bootstrap` to refresh it.

## Your role

You are the **orchestrator** for `dabbler-platform`: you do the mechanics — file
edits, shell, git — and the framework owns the lifecycle, one move at a time.
**Opened to consult, you are not the orchestrator**: read `dabbler consult
--sessions-dir docs/sessions` first, ask for no instruction, and commit what you change.

## How to run a session

Sessions are numbered under one sessions root (`docs/sessions/`), so no
command takes a handle to one.

**Start Session registers the session and opens your CLI.** You ask the
framework for an instruction once:

    dabbler session next --sessions-dir docs/sessions

It prints one instruction, as JSON, and exits. Do what its `ask` says, then
start its `answer_command` — running it is the answer — **as a background
command**, so this chat stays free for the operator. It stays open while the
framework checks, tests, reviews, commits and pushes, which can take many
minutes, and what it prints when it exits is your next instruction: act on
that one the same way, until one says `done`. Never poll it, and never start
anything else to wait with.

The operator can talk to you while it runs: answer them, and leave it
running. `dabbler status` says which instruction is owed, and for how long.

Outside VS Code the operator types the start: `dabbler session start
--sessions-dir docs/sessions --engine <engine> --provider <provider>` (a
seat adds `--model`). `dabbler session run --mailbox` is the older loop, a
fallback a person starts by hand; what it refuses says what to run under it.

## What comes back

Four kinds of instruction, and no fifth:

- **`step`** — work to do. Its `ask` says what; do it, then report with
  the `answer_command`. `--files` may be left out, and the framework takes
  the step's files from what changed; named, it lists every file you
  changed and nothing else.
- **`rejection`** — the answer was refused, and `reasons` says why. Fix
  it and answer again; three refusals of one step stop the session. A
  round's findings arrive as one too, and that one is ANSWERED rather
  than fixed: its `ask` says to change no file and to dispose of each.
- **`interrupt`** — the same instruction re-issued because something
  reached the framework while you worked; `reasons` carries it first,
  and the answer owed is the one you already owed.
- **`done`** — the session is over and closed. Stop.

**When an answer command exits, read what it printed.** A JSON instruction:
act on it. A line saying `refused`, or a stop (`Next:` and its ways on): do
what it says, and never repeat a stop. Anything else — it ended on progress
lines, or printed nothing — and it was cut off: start that exact command
again at once. An answer is taken once, the repeat carries on from where the
framework is, and the framework does not finish a session by itself.

Everything the framework does for itself happens between instructions:
declaring the work, each step's own checks, cross-provider verification and
its remediation rounds, the suites as the run of record, the commit, the
push, and the close. What runs is each step's own checks and the tests named
after what it changed, then the suites — whole, or the ones the session
selects where a whole run costs too much, and whole before a release; the
Primary Reviewer reviews without writing or running one. None of them is
yours to run or to skip ahead to — the instruction in hand is the whole of
what is asked, and `dabbler version` says which router this is. A source
file's tests are the file named after it (`checks.ts`, `checks.test.ts`): a
new public method gets a test there, a changed one has its tests updated.

**The framework owns the clock, the state and the sequencing.** An
instruction that names a command is answered by running that command —
never by watching `run.json` or any other record for what the framework
will do next. The answer command you started is the one thing you wait on.

**A release is a session of its own**, headed `(release: <version>)`: the
framework packs and publishes what is on the trunk; no other session
publishes. Propose each release's version (patch, minor or major) when you
write or amend the plan, for the person to approve with it. Never add a
`packaging:` block to `dabbler.yaml` until the next session is the packaging
session, which does nothing else; the close refuses an unpublished release.

## When the framework stops

- Read the framework's own account before the scrollback: `dabbler status`,
  the `stop` on `.dabbler/runs/s<N>/driver/run.json` with its kind and class,
  the outstanding instruction's `reasons`, and the transcripts.
- Where the framework is source in this tree you may fix it, and the fix
  rides in this session's own diff; where it is an installed package,
  report the step `blocked` with the diagnosis in its notes.
- Never touch the record, a verdict or a gate to get past a stop. The
  protocol is *When the framework stops* in `docs/driving-a-session.md`.

## Hard rules

- State files (`docs/sessions/sessions.json`) and everything under
  `.dabbler/runs/` are written by the router only — never by hand.
  The router commits the state files it writes at the land and the close,
  and a report never names those; it names `session-plan.md` if it edits it.
- Verdicts come from the **Primary Reviewer** -- *not the author* -- and a
  disputed impasse from the **Auxiliary Reviewer** -- *not the author and
  not the primary*. A verdict the framework did not hand you does not exist.
- API keys live in env vars (`DABBLER_ANTHROPIC_API_KEY`,
  `DABBLER_OPENAI_API_KEY`, `DABBLER_GEMINI_API_KEY`), never in files; the
  same rule covers a feed PAT, which configuration names and never holds.
- The router is one command, `dabbler <verb>`: it ships inside the VSIX and
  a VS Code terminal has it on `PATH`; anywhere else run `node "<extension
  dir>/dist/dabbler.cjs" <verb>`. "command not found" is PATH, not keys.
- `cancel --force` and `close --force` are a person's, never yours. The session you are working is yours to cancel: `dabbler session cancel <its number> --reason "<why>"`.
- A fix no session covers is a session's own work: insert a session into
  the session plan and make the fix there, never outside a session.

## Writing files

**Write files with your editing tools, never with a shell heredoc.** On a
Windows host the shell is usually Git Bash, and a heredoc there eats
backslashes: `\n` arrives as a newline and `\\` as one backslash, so JSON
escapes, regular expressions and Windows paths are silently corrupted on the
way to disk. Nothing fails — the file is written, and it is wrong. The same
goes for `echo` and for `printf` with a format you did not escape twice.

**Nothing may touch the working tree between a report and the
instruction that follows it.** The framework hashes the tree before and
after a step's checks, and an edit made while one is running refuses the
report — correctly, because a check run against a tree that moved under
it proves nothing about either version. Finish the step, report it, and
wait for the next instruction before starting the next step.

---

## Engine tail (GitHub Copilot)

You read this `AGENTS.md` directly. `CLAUDE.md` imports it rather than
repeating it, so this file is the one place the body exists. GitHub
Copilot loads both files at once and de-duplicates nothing, which is
exactly why only this one carries the body.

Copilot seats: declare `--model` on the first call, the one that
registers, and set the vehicle with `dabbler configure --transport
copilot-cli` when routing through the seat. If `DABBLER_TRANSPORT` is still
set in your environment, unset it: nothing reads it. Review stays
cross-provider on every transport.

<!-- dabbler:managed:end -->
