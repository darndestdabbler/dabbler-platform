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
