# Project Status — AI-Led Development Prototype

> **Purpose:** Capture everything a fresh Claude needs to know about the
> current state of the prototype after the BATON v2 deliverable package
> completed. Supersedes BATON v2 and Session 2–9 handoffs/briefs, all of
> which were bootstrapping artifacts whose durable content has landed in
> the files named under "Where durable content lives now" below.
> **Read order:** This file first, then the authoritative files it points
> to. Do not read the retired handoff/brief documents — if something from
> them were still load-bearing, it would be restated here.

---

## Project in one paragraph

A prototype for AI-led software development at a State government. The
workflow (spec → sessions → cross-provider verification → commit → E2E
tests → human UAT → change log → lessons learned) is the innovation; the
custom Python `ai-router/` module is scaffolding that proves the workflow
discipline is real and tractable. The same workflow can run on a
commercial platform (Copilot Enterprise, Claude Code, Cursor Business) or
a hybrid; the production choice is evaluated against production criteria
(security, effectiveness, efficiency, implementation transparency,
agility) rather than assumed from provenance. The audience for the
prototype's outputs is three State review boards — architecture,
security, and enterprise application — and the voice across the project
favors boring, procurement-friendly answers over clever ones.

---

## Where durable content lives now

Everything below is the authoritative source for its topic. Handoffs and
briefs have been retired.

| Topic | Authoritative file |
|---|---|
| Durable strategic commitments (principles, conventions) | `docs/planning/project-guidance.md` |
| Practical lessons from execution | `docs/planning/lessons-learned.md` |
| Session-set workflow — procedure, rules, router usage, verification, UAT, close-out | `docs/ai-led-session-workflow.md` |
| Board-facing governance narrative | `docs/planning/governance-two-pager.md` |
| Governance two-pager section outline (rationale for section order, "Section 3 is load-bearing") | `docs/planning/governance-two-pager-outline.md` |
| Orchestrator bootstrap (API keys, import snippets) | `CLAUDE.md`, `AGENTS.md`, `GEMINI.md` |
| Router design rationale (rules-first verifier selection, per-task `on_disagreement`, complexity-heuristic stopping point) | Comment blocks inline in `ai-router/router-config.yaml` |
| LLM instructions used for every routed task | `ai-router/prompt-templates/system-prompts.md`, `task-prompts.md`, `verification.md` |
| UAT checklist schema and editor behavior | `uat-checklist-editor` repo (separate): `checklist-schema.json`, `index.html`, `README.md`, `sample-checklist.json` |

If a fact is not in one of these files, treat it as unresolved rather than
reconstructing it from a retired handoff.

---

## Board order in the governance two-pager

The live two-pager uses `Security → Architecture → Enterprise Application`
for Sections 6–8. The companion outline document lists them in the order
`Architecture → Security → Enterprise Application` — that is the earlier
ordering from Session 1 and was reversed during Session 7 drafting.
Authoritative ordering is what is in the two-pager itself. If the outline
is re-used for any future revision, correct the ordering there too.

---

## Canonical repo layout

```
<repo-root>/
├── CLAUDE.md
├── AGENTS.md
├── GEMINI.md
├── ai-router/
│   ├── __init__.py
│   ├── config.py
│   ├── metrics.py
│   ├── models.py
│   ├── notifications.py
│   ├── prompting.py
│   ├── providers.py
│   ├── report.py
│   ├── requirements.txt
│   ├── router-config.yaml
│   ├── router-metrics.jsonl        (runtime log, not checked in fresh)
│   ├── session_log.py
│   ├── utils.py
│   ├── verification.py
│   └── prompt-templates/
│       ├── system-prompts.md
│       ├── task-prompts.md
│       └── verification.md
└── docs/
    ├── ai-led-session-workflow.md
    └── planning/
        ├── project-status.md                        (this file)
        ├── project-guidance.md
        ├── lessons-learned.md
        ├── governance-two-pager-outline.md
        ├── governance-two-pager.md
        └── governance-slides-outline.md             (if/when Session 8 scope runs)
```

The UAT checklist editor (`checklist-schema.json`, `index.html`,
`README.md`, `sample-checklist.json`) lives in the separate
`darndestdabbler/uat-checklist-editor` repo.

---

## Recommended Claude Project upload subset

When starting a new Claude Project for continuing work, the repo clone is
the source of truth for code; project knowledge is the source of truth
for durable workflow and governance context. A compact upload set is
usually sufficient:

- The three orchestrator instruction files (`CLAUDE.md`, `AGENTS.md`, `GEMINI.md`)
- `docs/ai-led-session-workflow.md`
- `docs/planning/project-status.md` (this file)
- `docs/planning/project-guidance.md`
- `docs/planning/lessons-learned.md`
- `docs/planning/governance-two-pager.md` (and the outline if space)
- `ai-router/router-config.yaml`
- The three prompt-template files

Python modules are uploaded only when a session actually edits them.

---

## Working voice (carried forward)

The discipline below was developed across sessions and is worth keeping
visible even though it partly duplicates material in `project-guidance.md`:

- **Don't over-engineer.** Multi-verifier consensus voting, auto-promotion
  trust timers, interactive step authoring in the UAT editor — all
  considered and correctly rejected when a simpler answer existed.
- **Don't propose new custom tools.** Extensions to existing files and
  existing tools are preferred over new surfaces that would need
  procurement review.
- **Push back when warranted.** Real pushback with reasoning is more
  valuable than agreement. Cross-provider review itself sometimes
  surfaces incorrect findings; verifying before implementing is part of
  the discipline.
- **Favor procurement-friendly answers over clever ones.** The three
  review boards are the audience; boring answers win.
- **Budget tool calls carefully.** Multi-session work benefits from
  clean partial handoffs over rushed completion in one session.

