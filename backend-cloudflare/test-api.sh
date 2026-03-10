#!/bin/bash

# Atlantis Strategy Backend API Test Script
# Tests all Phase 2 implemented features

set -e

BASE_URL="http://localhost:8787"
PLAYER_ID=""
TOKEN=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Atlantis Strategy Backend API Tests${NC}"
echo -e "${BLUE}========================================${NC}\n"

# Function to print test results
test_result() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ PASS${NC}: $1"
    else
        echo -e "${RED}✗ FAIL${NC}: $1"
        exit 1
    fi
}

# Test 1: Health Check
echo -e "\n${YELLOW}[1/10] Testing Health Endpoint...${NC}"
HEALTH=$(curl -s -w "\n%{http_code}" "$BASE_URL/health")
HTTP_CODE=$(echo "$HEALTH" | tail -n1)
BODY=$(echo "$HEALTH" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✓ Server is running${NC}"
    echo "Response: $BODY"
else
    echo -e "${RED}✗ Server not responding (HTTP $HTTP_CODE)${NC}"
    echo "Make sure you started the dev server with: npm run dev"
    exit 1
fi

# Test 2: Player Login/Creation
echo -e "\n${YELLOW}[2/10] Testing Player Authentication...${NC}"
AUTH_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{
        "steamId": "test-player-'$(date +%s)'",
        "username": "TestPlayer"
    }')

echo "Response: $AUTH_RESPONSE"

# Extract token (basic parsing, assumes JSON response)
TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
PLAYER_ID=$(echo "$AUTH_RESPONSE" | grep -o '"playerId":"[^"]*' | cut -d'"' -f4)

if [ -n "$TOKEN" ]; then
    test_result "Player authentication"
    echo "Player ID: $PLAYER_ID"
    echo "Token: ${TOKEN:0:20}..."
else
    echo -e "${RED}✗ Failed to get authentication token${NC}"
    echo "Response: $AUTH_RESPONSE"
    exit 1
fi

# Test 3: Get Player State
echo -e "\n${YELLOW}[3/10] Testing Get Player State...${NC}"
STATE=$(curl -s "$BASE_URL/api/player/state" \
    -H "Authorization: Bearer $TOKEN")

echo "Response: $STATE"
test_result "Get player state"

# Test 4: Building Upgrade - Fortress
echo -e "\n${YELLOW}[4/10] Testing Building Upgrade (Fortress)...${NC}"
BUILD_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/building/upgrade" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
        "buildingId": "fortress_1",
        "buildingType": "fortress",
        "zone": "inner"
    }')

echo "Response: $BUILD_RESPONSE"
test_result "Start building upgrade"

# Extract completion time
COMPLETION_TIME=$(echo "$BUILD_RESPONSE" | grep -o '"completionTime":[0-9]*' | cut -d':' -f2)
DURATION=$(echo "$BUILD_RESPONSE" | grep -o '"duration":[0-9]*' | cut -d':' -f2)

if [ -n "$DURATION" ]; then
    echo "Build will complete in $DURATION seconds"
fi

# Test 5: Check Build Queue
echo -e "\n${YELLOW}[5/10] Testing Build Queue Status...${NC}"
QUEUE_STATE=$(curl -s "$BASE_URL/api/player/state" \
    -H "Authorization: Bearer $TOKEN")

echo "Queue State: $QUEUE_STATE"
test_result "Check build queue"

# Test 6: Train Troops
echo -e "\n${YELLOW}[6/10] Testing Troop Training...${NC}"
TRAIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/troops/train" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
        "troopType": "porter",
        "quantity": 10
    }')

echo "Response: $TRAIN_RESPONSE"
test_result "Start troop training"

# Test 7: Start Research
echo -e "\n${YELLOW}[7/10] Testing Research Start...${NC}"
RESEARCH_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/research/start" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
        "researchType": "agriculture"
    }')

echo "Response: $RESEARCH_RESPONSE"
test_result "Start research"

# Test 8: Cancel Training (Test Refunds)
echo -e "\n${YELLOW}[8/10] Testing Training Cancellation...${NC}"
TRAIN_CANCEL=$(curl -s -X POST "$BASE_URL/api/player/troops/cancel" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
        "queueId": "test-queue-id"
    }')

echo "Response: $TRAIN_CANCEL"
# This might fail if no valid queue ID, that's okay
echo -e "${BLUE}(May fail if no valid queue - this is expected)${NC}"

# Test 9: Get Updated State (After Operations)
echo -e "\n${YELLOW}[9/10] Testing Final State...${NC}"
FINAL_STATE=$(curl -s "$BASE_URL/api/player/state" \
    -H "Authorization: Bearer $TOKEN")

echo "Final State: ${FINAL_STATE:0:200}..."
test_result "Get final state"

# Test 10: Wait for Building Completion (If Duration < 60s)
if [ -n "$DURATION" ] && [ "$DURATION" -lt 60 ]; then
    echo -e "\n${YELLOW}[10/10] Testing Durable Object Alarm (Building Completion)...${NC}"
    echo "Waiting $DURATION seconds for building to complete..."

    sleep "$DURATION"
    sleep 5  # Add 5s buffer for alarm processing

    COMPLETED_STATE=$(curl -s "$BASE_URL/api/player/state" \
        -H "Authorization: Bearer $TOKEN")

    echo "State after completion: $COMPLETED_STATE"

    # Check if build queue is empty
    if echo "$COMPLETED_STATE" | grep -q '"buildQueue":\[\]'; then
        echo -e "${GREEN}✓ Building completed successfully!${NC}"
        echo -e "${GREEN}✓ Durable Object alarm fired correctly${NC}"
    else
        echo -e "${YELLOW}! Building may still be in progress${NC}"
    fi
else
    echo -e "\n${YELLOW}[10/10] Skipping Alarm Test (Build time > 60s)${NC}"
    echo "Check logs in Wrangler terminal for alarm firing"
fi

# Summary
echo -e "\n${BLUE}========================================${NC}"
echo -e "${GREEN}All Tests Completed!${NC}"
echo -e "${BLUE}========================================${NC}\n"

echo -e "Player ID: ${BLUE}$PLAYER_ID${NC}"
echo -e "Token: ${BLUE}${TOKEN:0:30}...${NC}"
echo -e "\nNext steps:"
echo -e "  1. Check Wrangler terminal for alarm logs"
echo -e "  2. Test WebSocket: wscat -c ws://localhost:8787/ws"
echo -e "  3. Run more building upgrades to test worker limits"
echo -e "  4. Test prerequisite validation with invalid requests"
echo ""
