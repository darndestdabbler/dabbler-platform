# Mechanic log

Entries written by the Mechanic (a fresh instance opened with `dabbler consult
--mechanic` when a session is stuck). Each says what was changed and why; the
framework lands the change with the session's own diff.

## 2026-11-09 -- Session 17, plan phase

**Symptom.** Session 17 was in phase `plan` with no stop on the record; the
framework was waiting on the author for the plan answer. The author's harness
(auto-mode classifier) blocked the answer command because step 1's check
dropped the developer's LocalDB database `AcmeCoffeeOnline_Dev` before running
`AcmeCoffeeOnline.Api.Tests` twice.

**Fix.** In the author's plan file (`.dabbler/scratch/plan-s17.json`, untracked)
step `serialize-api-tests`'s check now sets
`ConnectionStrings__DefaultConnection` to a throwaway database,
`AcmeCoffeeOnline_Test`, drops that database instead, and runs the project
twice as before; the step's `ask` says so. Nothing else in the plan changed.

**Why this is the minimum.** The session plan's purpose for the check is a run
against a database that does not yet exist. A throwaway name proves that at
least as well as the developer's database and loses no data. Verified before
handing back: with the variable set, one run of the project on the unfixed tree
created `AcmeCoffeeOnline_Test` and reproduced the race the session plan
describes (`Database 'AcmeCoffeeOnline_Test' already exists`, duplicate keys in
`dbo.Customers`); `AcmeCoffeeOnline_Dev` was not touched.

**Hand-back.** The author submits the amended plan with its answer command:
`dabbler session report --sessions-dir docs/sessions --seq 1 --next
--answer-file .dabbler/scratch/plan-s17.json`.

## 2026-11-09 -- Session 21, run of record

**Symptom.** The run of record ran the `e2e` suite whole (`tools/e2e.ps1`),
it failed 13 of 178 (the 12 failures Session 22 owns plus one intermittent
with the same cause), and the framework issued `fix-run-of-record`. The
author reported the step blocked: the suite is `required_for_close: false`
by the plan, fixing the tests is a non-goal, and loosening the suite would
touch a gate. The automatic Mechanic at 16:27 did nothing: its log holds
only an expired Claude login.

**Diagnosis.** A framework gap, not a defect in this session's work. In
router 3.29.0 `phaseRunOfRecord` runs every `expensive` suite and treats
any failure as a run-of-record failure, while the land (`landFacts`) and the
close (`judgeFreshness`) demand only suites with `required_for_close:
true`. So a red non-required suite can never pass the run of record, though
the close would accept it. Nothing in this tree fixes that without
weakening a gate (an allowlist in `tools/e2e.ps1`) or contradicting the
plan (undeclaring the suite); a `select` would not help either, because a
suite with no earlier whole run still runs whole.

**Fix.** Outside this tree, as the rules require: Sessions 301 and 302 are
inserted into `D:\Projects\dabbler-ai-orchestration`'s session plan (its
`docs/sessions/session-plan.md`, uncommitted, for the operator to review).
Session 301 makes the run of record record a red `required_for_close:
false` suite and carry on; Session 302 releases it as 3.29.1. No file of
this repository changed except this log.

**Hand-back.** Once 3.29.1 is installed, the operator asks the author to
run `dabbler session next`. The pending step is still `fix-run-of-record`;
the author reports it with this log as the only change, the framework
re-runs the checks, verification and the suites, and the fixed router
records the red `e2e` run and lands.
