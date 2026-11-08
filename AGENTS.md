# AGENTS.md -- Dabbler Platform

## Project overview

Integrated repo for the AcmeCoffeeOnline rebuild, the `Dabbler.*` libraries it
is built from, the `dotnet new` templates and the `dabbler-crud-slice` and
`dabbler-devidp` tools. Everything publishes to nuget.org from one solution.

Development mode: integrated source references under `src/Libs/`. Do not add a
new sibling-repo dependency unless the session plan requires one; the only one
today is `dabbler-uat-dsl` (see `solution-dependencies.json`).

## Required reading before every session

- `README.md` -- repo layout and build entry points
- `docs/platform-overview.md` -- what the platform is and ships
- `docs/planning/project-guidance.md` -- durable principles and conventions
- `docs/planning/lessons-learned.md` -- repo-specific failure patterns
- `docs/planning/session-set-authoring-guide.platform-addendum.md` -- Blazor /
  MudBlazor / Playwright / AcmeCoffeeOnline conventions

## Building and testing

```bash
dotnet build Dabbler.Platform.sln
dotnet test  Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m   # the `unit` suite in dabbler.yaml (-m:1: template tests share a cache)

# E2E (Playwright runs from the host against the app containers). Not in
# dabbler.yaml: it needs the Docker stack up and has no skip guard.
docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog (start once, leave up)
docker compose -f docker/docker-compose.app.yml   up --build -d # api + web (rebuild after code changes)
dotnet test tests/AcmeCoffeeOnline.Playwright/

# Packaging gate (what CI runs; `dabbler packaging` runs pack.ps1 the same way):
pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput
dotnet test tests/Dabbler.PublicSurface.Tests/
dotnet test tests/Dabbler.Packaging.Smoke/ --filter "FullyQualifiedName!~ServesBelowFiveHundred"
```

`Dabbler.Unit.slnf` is every test project except `AcmeCoffeeOnline.Playwright`
(needs Docker) and `Dabbler.Packaging.Smoke` (needs packed nupkgs and SQL
LocalDB). The tests in `Dabbler.PublicSurface.Tests`, `Dabbler.Templates.Tests`
and `Dabbler.CrudSlice.Tests` that need nupkgs skip when `pack-output/` is
absent and `DABBLER_PACK_OUTPUT` is unset.

Gate rule (`docs/MAINTAINING.md` → *Where every gate lives*): the `unit`
suite lives in `dabbler.yaml`, PublicSurface and Packaging.Smoke in CI's
*Validate And Pack* job, and Playwright is a manual gate, run before each
release tag and in every session that changes the reference app's UI or the
MudBlazor pin.

The compose stack is split: `docker-compose.infra.yml` holds the persistent dev
dependencies (sqlserver, mailhog, the `dabbler-dev` network) and stays up
across sessions. `docker-compose.app.yml` holds the rebuildable app services
(api, web). Playwright is intentionally not a compose service: run it from the
host against `http://localhost:5010`.

Publishing to nuget.org is the job of CI: push an annotated `nuget-v<version>`
tag and `.github/workflows/build.yml` publishes through trusted publishing
(OIDC). Bump `DabblerVersion` in `Directory.Build.props` first. There is no
`push` block in `dabbler.yaml` on purpose: no credential lives on a developer
machine.

The Playwright project restores `Dabbler.Uat.Dsl.*` from the sibling checkout
`../dabbler-uat-dsl/pack-output` (feed name `dabbler-uat-dsl-local` in
`tests/AcmeCoffeeOnline.Playwright/nuget.config`). Re-pack that repo after
pulling it. `dabbler deps check` compares the declaration with the build files.

## Solution structure

- `src/Apps/AcmeCoffeeOnline.Web/` -- Blazor Server web app (reference app)
- `src/Apps/AcmeCoffeeOnline.Api/` -- ASP.NET Core Web API (reference app)
- `src/Libs/` -- integrated Dabbler libraries, all packed
- `templates/Dabbler.Templates/` -- `dotnet new` templates (`dabbler-app`,
  `dabbler-min`, `dabbler-crud-service`, `dabbler-crud-page`)
- `tools/Dabbler.CrudSlice/` -- `Dabbler.CrudSlice.Tool` (`dabbler-crud-slice`);
  bundles `tools/Dabbler.SchemaApplicator`
- `tools/Dabbler.Security.DevIdp.Tool/` -- `dabbler-devidp`
- `tests/` -- xUnit, bUnit and Playwright
- `docker/` -- compose stack
- `docs/sessions/` -- the session plan and router-written records (current workflow)
- `docs/session-sets/` -- 47 directories of v1 session sets, read-only history

Code style conventions (namespaces, casing, nullable, `Async` suffix) live in
`docs/planning/project-guidance.md` -> Conventions -> Code Style.

<!-- dabbler:managed:start -->
# AI orchestrator instructions — `dabbler-platform`

> `AGENTS.md` is the single source of this managed body; `CLAUDE.md`
> imports it and adds only its engine tail. Do not hand-edit inside the
> fence; re-run `dabbler bootstrap` to refresh it.

## Your role

You are the **orchestrator** for `dabbler-platform`: you do the mechanics — file
edits, shell, git — and the framework owns the lifecycle, one move at a time.
**Opened to consult, you are not the orchestrator**: read `dabbler consult --sessions-dir docs/sessions` first, ask
for no instruction, and commit what you change -- but with `--mechanic`, commit nothing: the framework lands your fix.

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
- `cancel`, `rollback` and `close` are a person's, never yours: report the step blocked and say why, and the person cancels or rolls back from the Work Explorer.
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
