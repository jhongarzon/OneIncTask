#!/bin/bash
# End-to-end test script for OneIncTask on Azure
set +H  # disable bash history expansion (! character)

BASE="https://oneinc-nginx.grayisland-b07a4226.brazilsouth.azurecontainerapps.io"
CREDS="admin:password123"
PASS=0
FAIL=0

pass() { echo "  PASS: $1"; PASS=$((PASS+1)); }
fail() { echo "  FAIL: $1"; FAIL=$((FAIL+1)); }

echo "============================================"
echo "  OneIncTask E2E Test Suite"
echo "============================================"
echo ""

# --- Test 1: Health Check ---
echo "--- Test 1: Health Check ---"
HEALTH=$(curl -s -w "\n%{http_code}" "$BASE/health")
HEALTH_CODE=$(echo "$HEALTH" | tail -1)
HEALTH_BODY=$(echo "$HEALTH" | head -1)
if [ "$HEALTH_CODE" = "200" ]; then pass "Health endpoint returns 200"; else fail "Health endpoint returned $HEALTH_CODE"; fi

# --- Test 2: Auth Token ---
echo "--- Test 2: Auth Token ---"
AUTH_RESP=$(curl -s -w "\n%{http_code}" -u "$CREDS" "$BASE/api/auth/token")
AUTH_CODE=$(echo "$AUTH_RESP" | tail -1)
AUTH_BODY=$(echo "$AUTH_RESP" | head -1)
TOKEN=$(echo "$AUTH_BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
if [ "$AUTH_CODE" = "200" ] && [ -n "$TOKEN" ]; then pass "Auth token received"; else fail "Auth token failed ($AUTH_CODE)"; fi

# --- Test 3: Frontend Assets ---
echo "--- Test 3: Frontend Assets ---"
HTML_SIZE=$(curl -s -u "$CREDS" -o /dev/null -w "%{size_download}" "$BASE/")
JS_SIZE=$(curl -s -o /dev/null -w "%{size_download}" "$BASE/assets/index-BLh-rLO3.js")
CSS_SIZE=$(curl -s -o /dev/null -w "%{size_download}" "$BASE/assets/index-BYY1QZG_.css")
echo "  HTML: ${HTML_SIZE}B, JS: ${JS_SIZE}B, CSS: ${CSS_SIZE}B"
if [ "$JS_SIZE" -gt 200000 ]; then pass "JS bundle loads fully ($JS_SIZE bytes)"; else fail "JS bundle truncated ($JS_SIZE bytes)"; fi
if [ "$CSS_SIZE" -gt 10000 ]; then pass "CSS bundle loads fully ($CSS_SIZE bytes)"; else fail "CSS bundle truncated ($CSS_SIZE bytes)"; fi

# --- Test 4: Start Job ---
echo "--- Test 4: Start Job (Hello, World!) ---"
JOB_RESP=$(curl -s -w "\n%{http_code}" -X POST "$BASE/api/jobs" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"inputText":"Hello, World!"}')
JOB_CODE=$(echo "$JOB_RESP" | tail -1)
JOB_BODY=$(echo "$JOB_RESP" | head -1)
JOB_ID=$(echo "$JOB_BODY" | grep -o '"jobId":"[^"]*"' | cut -d'"' -f4)
echo "  Job ID: $JOB_ID"
if [ "$JOB_CODE" = "202" ] && [ -n "$JOB_ID" ]; then pass "Job created (202 Accepted)"; else fail "Job creation failed ($JOB_CODE): $JOB_BODY"; fi

# --- Test 5: Poll Job Status Until Complete ---
echo "--- Test 5: Poll Job Progress ---"
# Status codes: 0=Pending, 1=Running, 2=Completed, 3=Cancelled, 4=Failed
status_label() {
  case "$1" in
    0) echo "Pending" ;; 1) echo "Running" ;; 2) echo "Completed" ;;
    3) echo "Cancelled" ;; 4) echo "Failed" ;; *) echo "Unknown($1)" ;;
  esac
}

EXPECTED_RESULT=" 1!1,1H1W1d1e1l3o2r1/SGVsbG8sIFdvcmxkIQ=="
MAX_WAIT=300
ELAPSED=0
FINAL_STATUS=""
FINAL_RESULT=""

while [ $ELAPSED -lt $MAX_WAIT ]; do
  sleep 5
  ELAPSED=$((ELAPSED+5))
  STATUS_RESP=$(curl -s "$BASE/api/jobs/$JOB_ID/status" -H "Authorization: Bearer $TOKEN")
  STATUS_NUM=$(echo "$STATUS_RESP" | grep -o '"status":[0-9]*' | cut -d: -f2)
  STATUS=$(status_label "$STATUS_NUM")
  PROCESSED=$(echo "$STATUS_RESP" | grep -o '"processedCharacters":[0-9]*' | cut -d: -f2)
  TOTAL=$(echo "$STATUS_RESP" | grep -o '"totalCharacters":[0-9]*' | cut -d: -f2)

  if [ -n "$TOTAL" ] && [ "$TOTAL" -gt 0 ] 2>/dev/null; then
    PCT=$((PROCESSED * 100 / TOTAL))
    echo "  [${ELAPSED}s] Status: $STATUS, Progress: $PROCESSED/$TOTAL ($PCT%)"
  else
    echo "  [${ELAPSED}s] Status: $STATUS"
  fi

  if [ "$STATUS" = "Completed" ] || [ "$STATUS" = "Failed" ] || [ "$STATUS" = "Cancelled" ]; then
    FINAL_STATUS="$STATUS"
    FINAL_RESULT=$(echo "$STATUS_RESP" | grep -o '"currentResult":"[^"]*"' | cut -d'"' -f4)
    break
  fi
done

if [ "$FINAL_STATUS" = "Completed" ]; then pass "Job completed successfully"; else fail "Job ended with status: $FINAL_STATUS"; fi
echo "  Expected: $EXPECTED_RESULT"
echo "  Got:      $FINAL_RESULT"
if [ "$FINAL_RESULT" = "$EXPECTED_RESULT" ]; then pass "Result matches expected output"; else fail "Result mismatch"; fi

# --- Test 6: Job History ---
echo "--- Test 6: Job History ---"
HIST_RESP=$(curl -s -w "\n%{http_code}" "$BASE/api/jobs/history" -H "Authorization: Bearer $TOKEN")
HIST_CODE=$(echo "$HIST_RESP" | tail -1)
HIST_BODY=$(echo "$HIST_RESP" | head -1)
if [ "$HIST_CODE" = "200" ]; then pass "Job history returns 200"; else fail "Job history failed ($HIST_CODE)"; fi
JOB_COUNT=$(echo "$HIST_BODY" | grep -o '"jobId"' | wc -l)
echo "  Jobs in history: $JOB_COUNT"
if [ "$JOB_COUNT" -ge 1 ]; then pass "History contains jobs"; else fail "History is empty"; fi

# --- Test 7: Start & Cancel Job ---
echo "--- Test 7: Start & Cancel Job ---"
CANCEL_RESP=$(curl -s -w "\n%{http_code}" -X POST "$BASE/api/jobs" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"inputText":"This is a longer text that should take more time to process so we can cancel it midway through"}')
CANCEL_CODE=$(echo "$CANCEL_RESP" | tail -1)
CANCEL_BODY=$(echo "$CANCEL_RESP" | head -1)
CANCEL_JOB_ID=$(echo "$CANCEL_BODY" | grep -o '"jobId":"[^"]*"' | cut -d'"' -f4)
echo "  Cancel test Job ID: $CANCEL_JOB_ID"

if [ "$CANCEL_CODE" = "202" ] && [ -n "$CANCEL_JOB_ID" ]; then
  pass "Cancel-test job created"

  # Wait a bit for processing to start
  sleep 8

  # Cancel the job
  DEL_RESP=$(curl -s -w "\n%{http_code}" -X DELETE "$BASE/api/jobs/$CANCEL_JOB_ID" -H "Authorization: Bearer $TOKEN")
  DEL_CODE=$(echo "$DEL_RESP" | tail -1)
  if [ "$DEL_CODE" = "200" ] || [ "$DEL_CODE" = "204" ]; then pass "Job cancelled successfully"; else fail "Cancel returned $DEL_CODE"; fi

  # Verify status
  sleep 2
  CSTATUS_RESP=$(curl -s "$BASE/api/jobs/$CANCEL_JOB_ID/status" -H "Authorization: Bearer $TOKEN")
  CSTATUS_NUM=$(echo "$CSTATUS_RESP" | grep -o '"status":[0-9]*' | cut -d: -f2)
  CSTATUS=$(status_label "$CSTATUS_NUM")
  echo "  Cancelled job status: $CSTATUS"
  if [ "$CSTATUS" = "Cancelled" ]; then pass "Job status is Cancelled"; else fail "Job status is $CSTATUS (expected Cancelled)"; fi
else
  fail "Cancel-test job creation failed ($CANCEL_CODE): $CANCEL_BODY"
fi

# --- Test 8: Final History Check ---
echo "--- Test 8: Final History Check ---"
HIST2_RESP=$(curl -s "$BASE/api/jobs/history" -H "Authorization: Bearer $TOKEN")
JOB_COUNT2=$(echo "$HIST2_RESP" | grep -o '"jobId"' | wc -l)
echo "  Jobs in history: $JOB_COUNT2"
if [ "$JOB_COUNT2" -ge 2 ]; then pass "History shows all jobs"; else fail "History missing jobs (got $JOB_COUNT2)"; fi

# --- Summary ---
echo ""
echo "============================================"
echo "  Results: $PASS passed, $FAIL failed"
echo "============================================"
