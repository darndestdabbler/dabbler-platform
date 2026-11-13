# Access-Migration Generator Consumption Spec

> **Purpose:** Close the one packaging gap the Access-migration pipeline still
> hits: the **code generators** `Dabbler.CrudSlice` and `Dabbler.SchemaApplicator`
> (under `tools/`) are **not** consumable as artifacts. The locked NuGet feed work
> (`dabbler-nuget-feed-publication`) deliberately publishes the **runtime**
> `Dabbler.*` libraries (the 15-package allowlist) and excludes these build-time
> generators. So a consumer that needs to *generate* a slice — chiefly the
> `dabbler-access-migration-orchestrator` at its playbook Step 7 — must clone and
> build this repo from source. Decide and implement how that consumer acquires the
> generators without a platform source checkout.
> **Created:** 2026-06-20 (requested by the operator while wiring the migration
> orchestrator front-door playbook; Step 00/Step 7 currently pin the generators as
> "source checkout, not on NuGet").
> **Session Set:** `docs/session-sets/access-migration-generator-consumption/`
> **Prerequisite:** `dabbler-nuget-feed-publication` complete (this set builds on
> the locked feed target, versioning scheme, and publish mechanics; it does **not**
> relitigate them).
> **Source of truth (re-confirm before editing):** `tools/Dabbler.CrudSlice/` and
> `tools/Dabbler.SchemaApplicator/` (the generator projects + their READMEs);
> `docs/session-sets/dabbler-nuget-feed-publication/feed-publication-decision.md`
> and that set's spec (the locked feed/versioning/auth decisions + the package
> allowlist); `docs/session-sets/packaging-and-template-readiness/` (the packable
> surface + `tools/pack.ps1` + versioning baseline); the
> `Dabbler.Security.DevIdp.Tool` project (the in-repo `dotnet tool` precedent); and,
> cross-repo, `../dabbler-access-migration-orchestrator/docs/planning/platform-output-contract.md`
> (which anchors to these generators by `tools/...` path) plus that repo's
> `docs/playbooks/migration-playbook.md` (Step 00 + Step 7).
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification.

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
tier: full
prerequisites: [dabbler-nuget-feed-publication]
totalSessions: 2
```

> Rationale: packaging/distribution infrastructure with no new user-visible UI
> surface, so no UAT/E2E. The acceptance proof is a **consumer-validation gate**:
> on a clean machine with no platform source checkout, acquire the generator(s) by
> the chosen mechanism and generate a CRUD slice from a sample `.dabbler-entity.json`
> that compiles. Cross-provider verification per session; Session 1 is a
> hard-to-reverse distribution decision and routes as `architecture` + tri-provider
> review, mirroring the feed set's decision session.

---

## Why This Set, Why Now

The platform's NuGet publishing work (`packaging-and-template-readiness` →
`dabbler-nuget-feed-publication`) solved the **runtime** consumption gap: a
generated app restores `Dabbler.*` libraries from nuget.org instead of vendoring
`.nupkg` files. But the **generation** step is upstream of that. The migration
orchestrator's deterministic lane (playbook Step 7) runs `Dabbler.CrudSlice` /
`Dabbler.SchemaApplicator` to turn a reviewed `.dabbler-entity.json` into POCO/DTO
+ CRUD scaffolding. Those generators live only as `tools/` source in this repo and
are **absent from the published package allowlist**, so today the orchestrator must
clone and build dabbler-platform to generate anything.

For the stated target — hundreds of Access databases, each generated in its own
repo/CI — "clone and build the platform to run the generator" is the same friction
the feed set removed for runtime libraries, one layer up. This set decides the
durable acquisition story for the generators and implements it.

This is deliberately **complementary to, and prerequisited on,** the feed set: it
adopts the locked feed target (nuget.org), versioning scheme, and publish
mechanics. It does not reopen those decisions; it only decides whether/how the
generators join that distribution story.

## Known constraints

- The generators are **build-time tools**, not app runtime dependencies — which is
  exactly why the feed allowlist excluded them. The decision must respect that
  framing rather than silently expanding the runtime publish set.
- A `dotnet tool` for a generator follows the established `Dabbler.Security.DevIdp.Tool`
  precedent and the parallel `Dabbler.AccessHarvester.Tool` work in the harvester
  repo, giving a consistent `dotnet tool install` acquisition story across the
  migration toolchain.
- Whatever is chosen must keep `platform-output-contract.md`'s anchoring valid: the
  orchestrator currently cites the generators by `tools/...` source path; a
  packaged artifact must not break that contract's traceability.

---

## Sessions

### Session 1 of 2: Decide the generator acquisition mechanism (architecture)
**Goal:** Choose, with a recorded rationale + tri-provider verdict, how an external
consumer acquires the generators without a platform source checkout.
**Options to weigh (pick one, name a close runner-up):**
- **`dotnet tool`(s)** — pack `Dabbler.CrudSlice` (and/or `SchemaApplicator`) as
  `dotnet tool`(s) on nuget.org, mirroring `Dabbler.Security.DevIdp.Tool` and the
  harvester tool. One-line install; framework-dependent.
- **Add to the feed allowlist as library/CLI packages** — extend the locked
  publish set, accepting that build-time tools then live in the runtime feed.
- **Keep source-checkout, formalize it** — document a pinned checkout/build step
  and leave the orchestrator playbook's source-checkout path as the supported one
  (the no-new-artifact option).
**Steps:** confirm the generators' current shape (entry points, RID/runtime needs,
asset bundling), weigh the options against the locked feed decisions, route the
decision through tri-provider review, and record it (decision doc) — no code yet.
**Ends with:** a recorded generator-acquisition decision consistent with the feed
architecture; the orchestrator playbook's Step 7/Step 00 acquisition note can then
be pinned to it.

### Session 2 of 2: Implement the chosen mechanism + consumer-validation gate
**Goal:** Ship the decision and prove a clean-machine consumer can generate.
**Steps:**
1. Implement the chosen packaging (e.g. `PackAsTool` + metadata, or feed-allowlist
   addition, or the documented checkout) using the existing version source and
   `tools/pack.ps1`/publish mechanics — no second versioning scheme.
2. Consumer-validation gate: on a clean checkout-free environment, acquire the
   generator by the chosen mechanism and generate a CRUD slice from a sample
   `.dabbler-entity.json` that builds green.
3. Update `platform-output-contract.md`-facing guidance and notify the
   orchestrator repo so its playbook Step 7/Step 00 `<CONFIRM>`/source-checkout
   note is replaced with the real acquisition command (cross-repo follow-up).
**Ends with:** the generators are acquirable by the decided mechanism, proven by a
clean-machine generation, with the orchestrator front door updatable to match.

## End-of-set deliverables

- A recorded generator-acquisition decision (tri-provider verified), consistent
  with the locked feed architecture.
- The chosen mechanism implemented (package/tool/feed-entry or formalized
  checkout), using the existing version source.
- A clean-machine consumer-validation proof that a CRUD slice generates without a
  platform source checkout.
- A cross-repo note enabling the migration orchestrator to pin its Step 7/Step 00
  generator-acquisition guidance.
