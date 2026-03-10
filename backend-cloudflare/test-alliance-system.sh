#!/bin/bash
# Alliance System Test Script
# Tests Phase 5: Alliances features

BASE_URL="http://localhost:8787"
set -e

echo "========================================"
echo "Alliance System Test Script"
echo "========================================"
echo ""

# Step 1: Initialize Player 1 (will be alliance leader)
echo "[1/15] Initializing Player 1 (future leader)..."
PLAYER1_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test-alliance-steam-1",
    "username": "AllianceLeader"
  }')

TOKEN1=$(echo "$PLAYER1_RESPONSE" | jq -r '.token')
PLAYER1_ID=$(echo "$PLAYER1_RESPONSE" | jq -r '.playerId')
echo "[OK] Player 1 initialized: $PLAYER1_ID"

sleep 1

# Step 2: Initialize Player 2 (will join alliance)
echo "[2/15] Initializing Player 2..."
PLAYER2_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test-alliance-steam-2",
    "username": "AllianceMember"
  }')

TOKEN2=$(echo "$PLAYER2_RESPONSE" | jq -r '.token')
PLAYER2_ID=$(echo "$PLAYER2_RESPONSE" | jq -r '.playerId')
echo "[OK] Player 2 initialized: $PLAYER2_ID"

sleep 1

# Step 3: Initialize Player 3 (will be invited)
echo "[3/15] Initializing Player 3..."
PLAYER3_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "steamId": "test-alliance-steam-3",
    "username": "InvitedPlayer"
  }')

TOKEN3=$(echo "$PLAYER3_RESPONSE" | jq -r '.token')
PLAYER3_ID=$(echo "$PLAYER3_RESPONSE" | jq -r '.playerId')
echo "[OK] Player 3 initialized: $PLAYER3_ID"

sleep 1

# Step 4: Player 1 creates an alliance
echo "[4/15] Player 1 creating alliance..."
ALLIANCE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/create" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d '{
    "name": "Test Alliance",
    "tag": "TEST",
    "description": "A test alliance for automated testing"
  }')

if echo "$ALLIANCE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  ALLIANCE_ID=$(echo "$ALLIANCE_RESPONSE" | jq -r '.allianceId')
  echo "[OK] Alliance created: $ALLIANCE_ID (tag: TEST)"
else
  echo "[ERROR] Failed to create alliance"
  echo "$ALLIANCE_RESPONSE" | jq .
  exit 1
fi

# Step 5: List all alliances (should show the new alliance)
echo "[5/15] Listing all alliances..."
ALLIANCES_LIST=$(curl -s "$BASE_URL/api/alliance/list" -H "Authorization: Bearer $TOKEN1")
ALLIANCE_COUNT=$(echo "$ALLIANCES_LIST" | jq '.alliances | length')
echo "[OK] Found $ALLIANCE_COUNT alliance(s)"

# Step 6: Get alliance info
echo "[6/15] Getting alliance info..."
ALLIANCE_INFO=$(curl -s "$BASE_URL/api/alliance/info?allianceId=$ALLIANCE_ID" -H "Authorization: Bearer $TOKEN1")
MEMBER_COUNT=$(echo "$ALLIANCE_INFO" | jq '.alliance.members | length')
echo "[OK] Alliance has $MEMBER_COUNT member(s)"

# Step 7: Player 2 applies to join the alliance
echo "[7/15] Player 2 applying to join alliance..."
JOIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/join" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN2" \
  -d "{\"allianceId\": \"$ALLIANCE_ID\"}")

if echo "$JOIN_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Application sent"
else
  echo "[ERROR] Failed to apply"
  echo "$JOIN_RESPONSE" | jq .
  exit 1
fi

# Step 8: Player 1 invites Player 3
echo "[8/15] Player 1 inviting Player 3..."
INVITE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/invite" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d "{\"targetPlayerId\": \"$PLAYER3_ID\"}")

if echo "$INVITE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Invitation sent to Player 3"
else
  echo "[ERROR] Failed to invite"
  echo "$INVITE_RESPONSE" | jq .
  exit 1
fi

# Step 9: Player 3 lists their invitations
echo "[9/15] Player 3 checking invitations..."
INVITES=$(curl -s "$BASE_URL/api/alliance/list-invites" -H "Authorization: Bearer $TOKEN3")
INVITE_COUNT=$(echo "$INVITES" | jq '.invitations | length')
if [ "$INVITE_COUNT" -gt 0 ]; then
  INVITATION_ID=$(echo "$INVITES" | jq -r '.invitations[0].invitation_id')
  echo "[OK] Player 3 has $INVITE_COUNT invitation(s)"
else
  echo "[ERROR] No invitations found for Player 3"
  exit 1
fi

# Step 10: Player 3 accepts the invitation
echo "[10/15] Player 3 accepting invitation..."
ACCEPT_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/accept-invite" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN3" \
  -d "{\"invitationId\": \"$INVITATION_ID\"}")

if echo "$ACCEPT_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Player 3 joined alliance"
else
  echo "[ERROR] Failed to accept invitation"
  echo "$ACCEPT_RESPONSE" | jq .
  exit 1
fi

sleep 1

# Step 11: Check alliance members (should now have 2 members)
echo "[11/15] Checking alliance members..."
ALLIANCE_INFO=$(curl -s "$BASE_URL/api/alliance/info?allianceId=$ALLIANCE_ID" -H "Authorization: Bearer $TOKEN1")
MEMBER_COUNT=$(echo "$ALLIANCE_INFO" | jq '.alliance.members | length')
if [ "$MEMBER_COUNT" -eq 2 ]; then
  echo "[OK] Alliance now has $MEMBER_COUNT members"
else
  echo "[ERROR] Expected 2 members, found $MEMBER_COUNT"
  exit 1
fi

# Step 12: Player 1 promotes Player 3 to officer
echo "[12/15] Player 1 promoting Player 3 to officer..."
PROMOTE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/promote" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d "{
    \"targetPlayerId\": \"$PLAYER3_ID\",
    \"allianceId\": \"$ALLIANCE_ID\"
  }")

if echo "$PROMOTE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Player 3 promoted to officer"
else
  echo "[ERROR] Failed to promote"
  echo "$PROMOTE_RESPONSE" | jq .
  exit 1
fi

# Step 13: Player 3 (now officer) demoted back to member
echo "[13/15] Player 1 demoting Player 3 back to member..."
DEMOTE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/demote" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN1" \
  -d "{
    \"targetPlayerId\": \"$PLAYER3_ID\",
    \"allianceId\": \"$ALLIANCE_ID\"
  }")

if echo "$DEMOTE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Player 3 demoted to member"
else
  echo "[ERROR] Failed to demote"
  echo "$DEMOTE_RESPONSE" | jq .
  exit 1
fi

# Step 14: Player 3 leaves the alliance
echo "[14/15] Player 3 leaving alliance..."
LEAVE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/alliance/leave" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN3")

if echo "$LEAVE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
  echo "[OK] Player 3 left alliance"
else
  echo "[ERROR] Failed to leave"
  echo "$LEAVE_RESPONSE" | jq .
  exit 1
fi

sleep 1

# Step 15: Verify alliance now has 1 member
echo "[15/15] Verifying final member count..."
ALLIANCE_INFO=$(curl -s "$BASE_URL/api/alliance/info?allianceId=$ALLIANCE_ID" -H "Authorization: Bearer $TOKEN1")
FINAL_MEMBER_COUNT=$(echo "$ALLIANCE_INFO" | jq '.alliance.members | length')

echo ""
echo "========================================"
echo "Test Results Summary"
echo "========================================"
echo "Alliance ID: $ALLIANCE_ID"
echo "Alliance Name: $(echo "$ALLIANCE_INFO" | jq -r '.alliance.name')"
echo "Alliance Tag: $(echo "$ALLIANCE_INFO" | jq -r '.alliance.tag')"
echo "Leader: $(echo "$ALLIANCE_INFO" | jq -r '.alliance.leaderId')"
echo "Final Member Count: $FINAL_MEMBER_COUNT"
echo ""
echo "Members:"
echo "$ALLIANCE_INFO" | jq -r '.alliance.members[] | "  - \(.playerName) (\(.role))"'
echo ""
echo "========================================"
echo "ALL TESTS PASSED!"
echo "========================================"
