#!/bin/bash
# Session 5 final-gate: run the entire Playwright suite N times in a row.
# Logs PASS/FAIL counts per run and total flakes.
#
# Usage: ./full-suite-stress.sh <iterations>

set -u

ITERATIONS="${1:-5}"
OUT="docs/session-sets/playwright-sync-watermark-and-playbook/full-suite-stress.log"
SUMMARY="docs/session-sets/playwright-sync-watermark-and-playbook/full-suite-stress.summary"

echo "=== Full Playwright suite stress: ${ITERATIONS} runs ===" > "$OUT"
echo "Started: $(date -Is)" >> "$OUT"
echo "" >> "$OUT"

PASS_RUNS=0
FAIL_RUNS=0
RESULTS=()

for i in $(seq 1 "$ITERATIONS"); do
    echo "--- Run $i/$ITERATIONS ($(date -Is)) ---" | tee -a "$OUT"

    RUN_OUT=$(dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj \
        --no-build --logger:"console;verbosity=normal" 2>&1)
    RC=$?

    LAST_LINE=$(echo "$RUN_OUT" | tail -20 | tr -d '\r')
    echo "$RUN_OUT" >> "$OUT"

    PASSED=$(echo "$LAST_LINE" | grep -oE 'Passed:\s*[0-9]+' | head -1 | grep -oE '[0-9]+' || echo 0)
    FAILED=$(echo "$LAST_LINE" | grep -oE 'Failed:\s*[0-9]+' | head -1 | grep -oE '[0-9]+' || echo 0)
    SKIPPED=$(echo "$LAST_LINE" | grep -oE 'Skipped:\s*[0-9]+' | head -1 | grep -oE '[0-9]+' || echo 0)

    if [ "$RC" -eq 0 ]; then
        PASS_RUNS=$((PASS_RUNS + 1))
        RESULTS+=("$i:PASS(P=$PASSED,F=$FAILED,S=$SKIPPED)")
        echo "Run $i: PASS (Passed=$PASSED Failed=$FAILED Skipped=$SKIPPED)" | tee -a "$OUT"
    else
        FAIL_RUNS=$((FAIL_RUNS + 1))
        RESULTS+=("$i:FAIL(P=$PASSED,F=$FAILED,S=$SKIPPED,exit=$RC)")
        echo "Run $i: FAIL (Passed=$PASSED Failed=$FAILED Skipped=$SKIPPED exit=$RC)" | tee -a "$OUT"
        # Capture failed test names for the summary
        echo "$RUN_OUT" | grep -E "^\s*Failed " | tee -a "$OUT"
    fi
    echo "" >> "$OUT"
done

echo "=== Summary ===" | tee -a "$OUT" "$SUMMARY"
echo "Iterations: $ITERATIONS" | tee -a "$OUT" "$SUMMARY"
echo "Pass runs: $PASS_RUNS" | tee -a "$OUT" "$SUMMARY"
echo "Fail runs: $FAIL_RUNS" | tee -a "$OUT" "$SUMMARY"
echo "Results: ${RESULTS[@]}" | tee -a "$OUT" "$SUMMARY"
echo "Finished: $(date -Is)" | tee -a "$OUT" "$SUMMARY"
