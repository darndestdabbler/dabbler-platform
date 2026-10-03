#!/bin/bash
# Session 5 diagnostic stress runner.
# Runs E2E_WF_04 N times against the running Web container, logging pass/fail
# and capturing the diagnostic dump on each failure. Output goes to the
# session set's diagnostics-NNN.log file for offline review.
#
# Usage: ./diagnostic-stress.sh <iterations> <output-suffix>
# Example: ./diagnostic-stress.sh 20 batch-1

set -u

ITERATIONS="${1:-20}"
SUFFIX="${2:-batch}"
OUT="docs/session-sets/playwright-sync-watermark-and-playbook/diagnostics-${SUFFIX}.log"
SUMMARY="docs/session-sets/playwright-sync-watermark-and-playbook/diagnostics-${SUFFIX}.summary"

PASS=0
FAIL=0
RESULTS=()

echo "=== Diagnostic stress: E2E_WF_04, ${ITERATIONS} iterations, suffix=${SUFFIX} ===" > "$OUT"
echo "Started: $(date -Is)" >> "$OUT"
echo "" >> "$OUT"

for i in $(seq 1 "$ITERATIONS"); do
    echo "--- Run $i/$ITERATIONS ($(date -Is)) ---" | tee -a "$OUT"

    # Use --logger:"console;verbosity=normal" so diagnostic Console.WriteLine
    # output is captured. dotnet test exit code is 0 on pass, non-zero on fail.
    dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj \
        --filter "FullyQualifiedName~E2E_WF_04_SystemAdmin_TogglesProductAvailability" \
        --no-build \
        --logger:"console;verbosity=normal" \
        2>&1 | tee -a "$OUT"

    RC=${PIPESTATUS[0]}
    if [ "$RC" -eq 0 ]; then
        PASS=$((PASS + 1))
        RESULTS+=("$i:PASS")
        echo "Run $i: PASS" >> "$OUT"
    else
        FAIL=$((FAIL + 1))
        RESULTS+=("$i:FAIL")
        echo "Run $i: FAIL (exit $RC)" >> "$OUT"
    fi
    echo "" >> "$OUT"
done

echo "=== Summary ===" | tee -a "$OUT" "$SUMMARY"
echo "Iterations: $ITERATIONS" | tee -a "$OUT" "$SUMMARY"
echo "Pass: $PASS" | tee -a "$OUT" "$SUMMARY"
echo "Fail: $FAIL" | tee -a "$OUT" "$SUMMARY"
echo "Results: ${RESULTS[@]}" | tee -a "$OUT" "$SUMMARY"
echo "Finished: $(date -Is)" | tee -a "$OUT" "$SUMMARY"
