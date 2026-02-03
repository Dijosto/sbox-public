#!/bin/bash

# Comprehensive Test Script for World Map & PvP System
# Tests: World generation, player spawning, map queries, NPC combat, PvP combat

set -e

BASE_URL="http://localhost:8787"
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}World Map & PvP Combat Test Suite${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Health Check
echo -e "${YELLOW}[1/12] Checking server health...${NC}"
curl -s "$BASE_URL/health" | jq '.'
echo ""

# Step 2: Create Player 1
echo -e "${YELLOW}[2/12] Creating Player 1 (Attacker)...${NC}"
PLAYER1_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test_player_001",
    "username": "Attacker"
  }')

echo "$PLAYER1_RESPONSE" | jq '.'
PLAYER1_TOKEN=$(echo "$PLAYER1_RESPONSE" | jq -r '.token')
PLAYER1_ID=$(echo "$PLAYER1_RESPONSE" | jq -r '.playerId')
echo -e "${GREEN}Player 1 Token: $PLAYER1_TOKEN${NC}"
echo -e "${GREEN}Player 1 ID: $PLAYER1_ID${NC}"
echo ""

# Step 3: Create Player 2
echo -e "${YELLOW}[3/12] Creating Player 2 (Defender)...${NC}"
PLAYER2_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test_player_002",
    "username": "Defender"
  }')

echo "$PLAYER2_RESPONSE" | jq '.'
PLAYER2_TOKEN=$(echo "$PLAYER2_RESPONSE" | jq -r '.token')
PLAYER2_ID=$(echo "$PLAYER2_RESPONSE" | jq -r '.playerId')
echo -e "${GREEN}Player 2 Token: $PLAYER2_TOKEN${NC}"
echo -e "${GREEN}Player 2 ID: $PLAYER2_ID${NC}"
echo ""

# Step 4: Get Player 1 State (including city coordinates)
echo -e "${YELLOW}[4/12] Getting Player 1 state (city location)...${NC}"
PLAYER1_STATE=$(curl -s "$BASE_URL/api/player/state" \
  -H "Authorization: Bearer $PLAYER1_TOKEN")

echo "$PLAYER1_STATE" | jq '.city.position'
PLAYER1_X=$(echo "$PLAYER1_STATE" | jq -r '.city.position.x')
PLAYER1_Y=$(echo "$PLAYER1_STATE" | jq -r '.city.position.y')
echo -e "${GREEN}Player 1 City: ($PLAYER1_X, $PLAYER1_Y)${NC}"
echo ""

# Step 5: Get Player 2 State (including city coordinates)
echo -e "${YELLOW}[5/12] Getting Player 2 state (city location)...${NC}"
PLAYER2_STATE=$(curl -s "$BASE_URL/api/player/state" \
  -H "Authorization: Bearer $PLAYER2_TOKEN")

echo "$PLAYER2_STATE" | jq '.city.position'
PLAYER2_X=$(echo "$PLAYER2_STATE" | jq -r '.city.position.x')
PLAYER2_Y=$(echo "$PLAYER2_STATE" | jq -r '.city.position.y')
echo -e "${GREEN}Player 2 City: ($PLAYER2_X, $PLAYER2_Y)${NC}"
echo ""

# Step 6: Query world tiles in Player 1's region
echo -e "${YELLOW}[6/12] Querying world map (Player 1's region)...${NC}"
REGION_X=$((PLAYER1_X / 100))
REGION_Y=$((PLAYER1_Y / 100))
echo -e "${BLUE}Region: ($REGION_X, $REGION_Y)${NC}"

curl -s "$BASE_URL/api/world/tiles?regionX=$REGION_X&regionY=$REGION_Y" | jq '{
  region: .region,
  tileCount: .count,
  cities: [.tiles[] | select(.tileType == "city")],
  npcCamps: [.tiles[] | select(.tileType == "npc_camp") | {x, y, level}] | .[0:3],
  wilderness: [.tiles[] | select(.tileType == "wilderness") | {x, y, resourceType, level}] | .[0:3]
}'
echo ""

# Step 7: Get specific tile details (Player 2's city)
echo -e "${YELLOW}[7/12] Getting tile details for Player 2's city...${NC}"
curl -s "$BASE_URL/api/world/tile/$PLAYER2_X/$PLAYER2_Y" | jq '.'
echo ""

# Step 8: Search for players by name
echo -e "${YELLOW}[8/12] Searching for players...${NC}"
curl -s "$BASE_URL/api/world/search?playerName=Player" | jq '.'
echo ""

# Step 9: Give Player 2 some troops for defense
echo -e "${YELLOW}[9/12] Training troops for Player 2 (Defender)...${NC}"
curl -s -X POST "$BASE_URL/api/player/troops/train" \
  -H "Authorization: Bearer $PLAYER2_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "troopType": "conscript",
    "quantity": 100
  }' | jq '.'

# Complete training instantly for testing
curl -s -X POST "$BASE_URL/api/player/troops/complete" \
  -H "Authorization: Bearer $PLAYER2_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' | jq '.' || true
echo ""

# Step 10: Give Player 1 attack troops
echo -e "${YELLOW}[10/12] Training troops for Player 1 (Attacker)...${NC}"
curl -s -X POST "$BASE_URL/api/player/troops/train" \
  -H "Authorization: Bearer $PLAYER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "troopType": "conscript",
    "quantity": 150
  }' | jq '.'

# Complete training instantly for testing
curl -s -X POST "$BASE_URL/api/player/troops/complete" \
  -H "Authorization: Bearer $PLAYER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' | jq '.' || true
echo ""

sleep 2

# Step 11: Send PvP march from Player 1 to Player 2
echo -e "${YELLOW}[11/12] Sending PvP march (Player 1 → Player 2)...${NC}"
echo -e "${BLUE}Attacker: ($PLAYER1_X, $PLAYER1_Y) → Defender: ($PLAYER2_X, $PLAYER2_Y)${NC}"

MARCH_RESPONSE=$(curl -s -X POST "$BASE_URL/api/player/march/send" \
  -H "Authorization: Bearer $PLAYER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"destination\": { \"x\": $PLAYER2_X, \"y\": $PLAYER2_Y },
    \"troops\": [
      { \"troopType\": \"conscript\", \"quantity\": 100 }
    ],
    \"marchType\": \"attack\",
    \"targetType\": \"player\"
  }")

echo "$MARCH_RESPONSE" | jq '.'
MARCH_ID=$(echo "$MARCH_RESPONSE" | jq -r '.marchId // empty')

if [ -z "$MARCH_ID" ]; then
  echo -e "${RED}Failed to send march!${NC}"
  exit 1
fi

echo -e "${GREEN}March ID: $MARCH_ID${NC}"
echo ""

# Wait for march to arrive
TRAVEL_TIME=$(echo "$MARCH_RESPONSE" | jq -r '.travelTime // 10')
echo -e "${BLUE}Waiting ${TRAVEL_TIME}s for march to arrive...${NC}"
sleep $((TRAVEL_TIME + 2))

# Step 12: Check battle reports and messages
echo -e "${YELLOW}[12/12] Checking battle results...${NC}"

echo -e "${BLUE}Player 1 (Attacker) Messages:${NC}"
curl -s "$BASE_URL/api/player/messages" \
  -H "Authorization: Bearer $PLAYER1_TOKEN" | jq '.messages[] | select(.message_type == "battle_report") | {subject, body, metadata}'
echo ""

echo -e "${BLUE}Player 2 (Defender) Messages:${NC}"
curl -s "$BASE_URL/api/player/messages" \
  -H "Authorization: Bearer $PLAYER2_TOKEN" | jq '.messages[] | select(.message_type == "battle_report") | {subject, body, metadata}'
echo ""

echo -e "${BLUE}Player 1 Final State:${NC}"
curl -s "$BASE_URL/api/player/state" \
  -H "Authorization: Bearer $PLAYER1_TOKEN" | jq '{
  resources: .resources,
  troops: .troops,
  activeMarches: .activeMarches | length
}'
echo ""

echo -e "${BLUE}Player 2 Final State:${NC}"
curl -s "$BASE_URL/api/player/state" \
  -H "Authorization: Bearer $PLAYER2_TOKEN" | jq '{
  resources: .resources,
  troops: .troops,
  activeMarches: .activeMarches | length
}'
echo ""

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Test Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
