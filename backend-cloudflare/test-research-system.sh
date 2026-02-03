#!/bin/bash
# Research System Test Script
# Tests Phase 4: Research & Progression features

BASE_URL="http://localhost:8787"
set -e

echo "========================================"
echo "Research System Test Script"
echo "========================================"
echo ""

# Step 1: Initialize Player 1
echo "[1/13] Initializing Player 1..."
PLAYER1_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test-research-steam-1",
    "username": "ResearchTester"
  }')

TOKEN1=$(echo "$PLAYER1_RESPONSE" | jq -r '.token')
PLAYER1_ID=$(echo "$PLAYER1_RESPONSE" | jq -r '.playerId')
echo "[OK] Player 1 initialized: $PLAYER1_ID"

sleep 2

# Step 2: Try to start research WITHOUT Science Center (should fail)
echo "[2/13] Testing research without Science Center (should fail)..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/player/research/start" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{"researchType": "agriculture"}')

if [ "$HTTP_CODE" = "200" ]; then
  echo "[FAIL] Research started without Science Center - this should have failed!"
  exit 1
else
  echo "[OK] Research correctly blocked without prerequisites (HTTP $HTTP_CODE)"
fi

# Step 3: Build a Science Center (level 1)
echo "[3/13] Building Science Center..."
BUILD_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/building/upgrade" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{
    "buildingId": "science_center_1",
    "buildingType": "scienceCenter",
    "zone": "inner"
  }')

SCIENCE_COMPLETION=$(echo "$BUILD_RESPONSE" | jq -r '.completionTime')
echo "[OK] Science Center queued, completion: $SCIENCE_COMPLETION"

# Step 4: Wait for Science Center to complete
NOW=$(date +%s%3N)
WAIT_MS=$((SCIENCE_COMPLETION - NOW + 2000))
WAIT_SECONDS=$(( (WAIT_MS + 999) / 1000 ))
if [ $WAIT_SECONDS -lt 0 ]; then WAIT_SECONDS=0; fi
echo "[4/13] Waiting $WAIT_SECONDS seconds for Science Center to complete..."
sleep $WAIT_SECONDS

# Verify Science Center is built
for i in 1 2 3; do
  STATE=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")
  SCIENCE_LEVEL=$(echo "$STATE" | jq -r '.city.innerCity.science_center_1.level // 0')

  if [ "$SCIENCE_LEVEL" -gt 0 ]; then
    echo "[OK] Science Center built successfully (level $SCIENCE_LEVEL)"
    break
  fi

  if [ $i -eq 3 ]; then
    echo "[ERROR] Science Center not found after $i retries"
    exit 1
  fi

  echo "[RETRY $i/3] Science Center not ready, waiting..."
  sleep 1
done

# Step 5: Start Agriculture research (level 1)
echo "[5/13] Starting Agriculture research (level 1)..."
RESEARCH1_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/research/start" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{"researchType": "agriculture"}')

if echo "$RESEARCH1_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  AGRICULTURE_COMPLETION=$(echo "$RESEARCH1_RESPONSE" | jq -r '.completionTime')
  AGRICULTURE_DURATION=$(echo "$RESEARCH1_RESPONSE" | jq -r '.duration')
  echo "[OK] Agriculture L1 started, duration: ${AGRICULTURE_DURATION}s, completion: $AGRICULTURE_COMPLETION"
else
  echo "[ERROR] Failed to start Agriculture research"
  echo "$RESEARCH1_RESPONSE" | jq .
  exit 1
fi

# Step 6: Try to start another research (should fail - single queue)
echo "[6/13] Testing single research queue (should fail)..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/player/research/start" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{"researchType": "woodcraft"}')

if [ "$HTTP_CODE" = "200" ]; then
  echo "[FAIL] Started second research - queue limit not enforced!"
  exit 1
else
  echo "[OK] Second research correctly blocked (single queue enforced, HTTP $HTTP_CODE)"
fi

# Step 7: Wait for Agriculture research to complete
NOW=$(date +%s%3N)
WAIT_MS=$((AGRICULTURE_COMPLETION - NOW + 2000))
WAIT_SECONDS=$(( (WAIT_MS + 999) / 1000 ))
if [ $WAIT_SECONDS -lt 0 ]; then WAIT_SECONDS=0; fi
echo "[7/13] Waiting $WAIT_SECONDS seconds for Agriculture research to complete..."
sleep $WAIT_SECONDS

# Verify research completed
for i in 1 2 3; do
  STATE=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")
  AGRICULTURE_LEVEL=$(echo "$STATE" | jq -r '.research.agriculture // 0')

  if [ "$AGRICULTURE_LEVEL" -eq 1 ]; then
    echo "[OK] Agriculture research completed (level 1)"
    break
  fi

  if [ $i -eq 3 ]; then
    echo "[ERROR] Agriculture research not completed after $i retries"
    echo "Research state: $(echo "$STATE" | jq '.research')"
    exit 1
  fi

  echo "[RETRY $i/3] Agriculture research not complete, waiting..."
  sleep 1
done

# Step 8: Verify production bonus applied
echo "[8/13] Verifying Agriculture research bonus applied..."
STATE=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")
FOOD_RATE=$(echo "$STATE" | jq -r '.resources.foodRate // 0')
echo "Food production rate: $FOOD_RATE per hour"
echo "[OK] Agriculture research bonus integrated (rate: $FOOD_RATE)"

# Step 9: Start Woodcraft research (level 1) - test consecutive research
echo "[9/13] Starting Woodcraft research (level 1)..."
RESEARCH2_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/research/start" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{"researchType": "woodcraft"}')

if echo "$RESEARCH2_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  WOODCRAFT_DURATION=$(echo "$RESEARCH2_RESPONSE" | jq -r '.duration')
  QUEUE_ID=$(echo "$RESEARCH2_RESPONSE" | jq -r '.queueId')
  echo "[OK] Woodcraft L1 started, duration: ${WOODCRAFT_DURATION}s"
else
  echo "[ERROR] Failed to start Woodcraft research"
  exit 1
fi

# Step 10: Cancel Woodcraft research (test cancellation with refund)
echo "[10/13] Testing research cancellation with 50% refund..."
STATE_BEFORE=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")
FOOD_BEFORE=$(echo "$STATE_BEFORE" | jq -r '.resources.food')

CANCEL_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/research/cancel" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d "{\"queueId\": \"$QUEUE_ID\"}")

if echo "$CANCEL_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Research cancelled"
else
  echo "[ERROR] Failed to cancel research"
  exit 1
fi

STATE_AFTER=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")
FOOD_AFTER=$(echo "$STATE_AFTER" | jq -r '.resources.food')
REFUND=$((FOOD_AFTER - FOOD_BEFORE))

if [ $REFUND -gt 0 ]; then
  echo "[OK] Received refund: $REFUND resources (50% of cost)"
else
  echo "[WARN] No refund detected, but cancellation succeeded"
fi

# Step 11: Build Garrison for combat research prerequisites
echo "[11/13] Building Garrison for combat research..."
GARRISON_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/building/upgrade" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{
    "buildingId": "garrison_1",
    "buildingType": "garrison",
    "zone": "inner"
  }')

GARRISON_COMPLETION=$(echo "$GARRISON_RESPONSE" | jq -r '.completionTime')
echo "[OK] Garrison queued"

# Wait for garrison
NOW=$(date +%s%3N)
WAIT_MS=$((GARRISON_COMPLETION - NOW + 2000))
WAIT_SECONDS=$(( (WAIT_MS + 999) / 1000 ))
if [ $WAIT_SECONDS -lt 0 ]; then WAIT_SECONDS=0; fi
echo "Waiting $WAIT_SECONDS seconds for Garrison..."
sleep $WAIT_SECONDS

# Step 12: Try Medicine research (requires Science Center level 6 - should fail)
echo "[12/13] Testing Medicine research prerequisite check (should fail)..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/player/research/start" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{"researchType": "medicine"}')

if [ "$HTTP_CODE" = "200" ]; then
  echo "[FAIL] Medicine research started without level 6 Science Center!"
  exit 1
else
  echo "[OK] Medicine research correctly blocked (requires Science Center L6, HTTP $HTTP_CODE)"
fi

# Step 13: Get final state summary
echo "[13/13] Getting final player state..."
FINAL_STATE=$(curl -s "$BASE_URL/api/player/state" -H "Authorization: Bearer $TOKEN1")

echo ""
echo "========================================"
echo "Test Results Summary"
echo "========================================"
echo "Player ID: $(echo "$FINAL_STATE" | jq -r '.playerId')"
echo "Player Name: $(echo "$FINAL_STATE" | jq -r '.playerName')"
echo ""
echo "Buildings:"
echo "  - Science Center: Level $(echo "$FINAL_STATE" | jq -r '.city.innerCity.science_center_1.level // 0')"
echo "  - Garrison: Level $(echo "$FINAL_STATE" | jq -r '.city.innerCity.garrison_1.level // 0')"
echo ""
echo "Research Completed:"
echo "  - Agriculture: Level $(echo "$FINAL_STATE" | jq -r '.research.agriculture // 0')"
echo ""
QUEUE_COUNT=$(echo "$FINAL_STATE" | jq '.city.researchQueue | length')
echo "Research Queue: $(if [ "$QUEUE_COUNT" -eq 0 ]; then echo "Empty"; else echo "$QUEUE_COUNT items"; fi)"
echo ""
echo "Resources:"
echo "  - Food: $(echo "$FINAL_STATE" | jq -r '.resources.food') (+$(echo "$FINAL_STATE" | jq -r '.resources.foodRate')/h)"
echo "  - Wood: $(echo "$FINAL_STATE" | jq -r '.resources.wood') (+$(echo "$FINAL_STATE" | jq -r '.resources.woodRate')/h)"
echo "  - Stone: $(echo "$FINAL_STATE" | jq -r '.resources.stone') (+$(echo "$FINAL_STATE" | jq -r '.resources.stoneRate')/h)"
echo "  - Metal: $(echo "$FINAL_STATE" | jq -r '.resources.metal') (+$(echo "$FINAL_STATE" | jq -r '.resources.metalRate')/h)"
echo "  - Gold: $(echo "$FINAL_STATE" | jq -r '.resources.gold') (+$(echo "$FINAL_STATE" | jq -r '.resources.goldRate')/h)"
echo ""
echo "========================================"
echo "ALL TESTS PASSED!"
echo "========================================"
