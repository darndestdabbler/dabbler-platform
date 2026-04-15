# AcmeCoffeeDistributors Reframe Spec

> **Purpose:** Reframe the sample application around the
> `AcmeCoffeeDistributors` domain so later security, workflow, and test work
> builds on the right story, names, and seeded personas.
> **Created:** 2026-04-15
> **Session Set:** `docs/session-sets/distributor-reframe/`
> **Prerequisite:** `stubbed-uat` is complete and any early design or
> navigation findings that materially affect domain presentation have been
> triaged.
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification

---

## Project Overview

This set replaces the older `AcmeCoffeeOnline` framing with a distributor to
retail-business story that better exercises the platform direction described in
Phase 2.

The domain reframe should land early and comprehensively enough that later
multi-organization security, coverage, E2E, and documentation work do not need
to keep translating between old and new narratives.

Treat this as a platform-first sample-app reframe. The result should be
presentation-worthy and internally coherent without drifting into
product-specific scaffolding that narrows the platform story too much.

## Parallelization Notes

No later Phase 2 session set is marked safe to overlap at authoring time.
Session 1 must still inspect whether the reframe reveals any genuinely safe
parallel lanes.

Inside this set, seed-data and naming-copy updates can usually proceed in
parallel with test and documentation clean-up once the canonical domain glossary
is frozen.

## Session Plan

### Session 1 of 3: Review, Domain Glossary, And Queue Scan

**Steps:**
1. Review the current application baseline, early UAT findings, and all
   remaining Phase 2 session-set specs before changing the app story.
2. Freeze the canonical domain glossary for `AcmeCoffeeDistributors`,
   retail-business organizations, internal staff, and seeded personas.
3. Identify any conservative adjustments needed in downstream specs, including
   any newly safe parallelization opportunities.
4. Capture the migration map from legacy `AcmeCoffeeOnline` terms and
   assumptions to the new distributor model.

This review should include the accepted `/platform/stub` demo route and its
supporting guidance so the reusable stub story moves to the distributor domain
alongside the main app narrative instead of staying on legacy naming.

**Creates:** Domain glossary and legacy-to-distributor migration map
**Touches:** This spec, downstream Phase 2 specs if needed, and planning notes
**Ends with:** The reframe has a stable vocabulary and downstream work can
depend on it
**Progress keys:** `session-1/review-scan`, `session-1/domain-glossary`,
`session-1/migration-map`, `session-1/parallelization-update`

---

### Session 2 of 3: App, Seed, And Navigation Reframe

**Steps:**
1. Reframe naming, navigation copy, seeded organizations, seeded users, and
   sample workflows around the distributor-to-retail-business model.
2. Represent retail businesses as organizations with multiple employee users
   and keep the configurable `internal` organization concept visible where
   appropriate.
3. Remove lingering `AcmeCoffeeOnline` assumptions from app content, developer
   guidance, and sample data where the new story should now lead.
4. Preserve the existing platform-first architecture while improving the
   sample-app coherence and presentation quality.

This includes any retained stub/demo route copy, reviewer guidance, and sample
stub content that still presents the older app story.

**Creates:** Distributor-aligned app and seed baseline
**Touches:** `src/Apps/`, seed or sample-data assets, and relevant docs
**Ends with:** The sample app tells the distributor story consistently enough
for later security and test work to build on it directly
**Progress keys:** `session-2/app-reframe`, `session-2/seed-reframe`,
`session-2/navigation-copy`, `session-2/platform-first-baseline`

---

### Session 3 of 3: Test Cleanup, Acceptance Baseline, And Closeout

**Steps:**
1. Remove or update tests, docs, and workflow references that still encode the
   old app story.
2. Capture the accepted distributor baseline so later session sets have a
   durable handoff.
3. Revisit the remaining Phase 2 queue and adjust only the assumptions that the
   reframe genuinely changed.
4. Generate `change-log.md` for this session set.

**Creates:** Distributor acceptance baseline and `change-log.md`
**Touches:** Tests, planning docs, downstream Phase 2 specs if needed, and
closeout artifacts
**Ends with:** Later security, coverage, E2E, and documentation work can rely
on one coherent distributor baseline
**Progress keys:** `session-3/test-cleanup`,
`session-3/acceptance-baseline`, `session-3/queue-rebaseline`,
`session-3/change-log`
