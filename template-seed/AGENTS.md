# AGENTS.md -- {{PROJECT_NAME}} (Codex)

Read `README.md` for the repo layout and `docs/ai-led-session-workflow.md` for
the session-set workflow.

## Project Rules

- work from the integrated source inside this repo
- do not add sibling-repo dependencies unless a session spec requires it
- log every session step with `SessionLog`
- use `route()` for reasoning tasks and external session verification
- complete exactly one session per conversation

## Build And Test

Update these commands for the new repo before the first real session:

```bash
dotnet build
dotnet test
```

## Session Trigger

When the human says "Start the next session":

1. verify API keys
2. identify the active session set
3. read the current session from `spec.md`
4. execute and log each step
5. run build/test
6. run mandatory external session verification
7. print the cost report, commit, and stop
