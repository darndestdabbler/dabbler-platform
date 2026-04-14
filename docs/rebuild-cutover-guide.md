# Rebuild Cutover Guide

## Canonical Repo

`dabbler-platform` is now the canonical development home for:

- AcmeCoffeeOnline app code
- required Dabbler library source
- the AI router and orchestrator instruction files
- active session-set artifacts and guardrail docs
- the reusable local workflow template seed

## Active Workflow

- run future work from this repo, not the older sibling repos
- treat `docs/session-sets/rebuild-consolidation/` as the completed rebuild record
- use `template-seed/` when bootstrapping the workflow into a new repo

## Archival Guidance

### `dabbler-demo`

- treat as historical reference only
- use it to compare behavior, not as a live code home
- do not add new platform fixes there unless a cutover issue requires temporary backport documentation

### `dabbler-security`

- treat as the pre-consolidation library source history
- use it only when auditing older design decisions or migration intent
- make all new security-library changes in `src/Libs/` here

### `dabbler-components`

- treat as archival source material for components not yet imported
- import only the packages or files justified by a session spec
- do not reintroduce sibling-repo source references into the integrated solution

## Cutover Checklist

- keep build/test commands anchored on `Dabbler.Platform.sln`
- prefer integrated source references over package/sibling drift
- update docs and root instructions here first whenever workflow rules change
- create new follow-on session sets in this repo so the activity log stays canonical
