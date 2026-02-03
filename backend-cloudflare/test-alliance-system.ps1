# Alliance System Test Script
# Tests Phase 5: Alliances features

$BaseUrl = "http://localhost:8787"
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Alliance System Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Initialize Player 1 (will be alliance leader)
Write-Host "[1/15] Initializing Player 1 (future leader)..." -ForegroundColor Yellow
$player1Body = @{
    steamId = "test-alliance-steam-1"
    username = "AllianceLeader"
} | ConvertTo-Json

try {
    $player1 = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $player1Body -ContentType "application/json"
    $token1 = $player1.token
    $player1Id = $player1.playerId
    $headers1 = @{ Authorization = "Bearer $token1" }
    Write-Host "[OK] Player 1 initialized: $player1Id" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to initialize player 1: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# Step 2: Initialize Player 2 (will join alliance)
Write-Host "[2/15] Initializing Player 2..." -ForegroundColor Yellow
$player2Body = @{
    steamId = "test-alliance-steam-2"
    username = "AllianceMember"
} | ConvertTo-Json

try {
    $player2 = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $player2Body -ContentType "application/json"
    $token2 = $player2.token
    $player2Id = $player2.playerId
    $headers2 = @{ Authorization = "Bearer $token2" }
    Write-Host "[OK] Player 2 initialized: $player2Id" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to initialize player 2: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# Step 3: Initialize Player 3 (will be invited)
Write-Host "[3/15] Initializing Player 3..." -ForegroundColor Yellow
$player3Body = @{
    steamId = "test-alliance-steam-3"
    username = "InvitedPlayer"
} | ConvertTo-Json

try {
    $player3 = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $player3Body -ContentType "application/json"
    $token3 = $player3.token
    $player3Id = $player3.playerId
    $headers3 = @{ Authorization = "Bearer $token3" }
    Write-Host "[OK] Player 3 initialized: $player3Id" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to initialize player 3: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# Step 4: Player 1 creates an alliance
Write-Host "[4/15] Player 1 creating alliance..." -ForegroundColor Yellow
$allianceBody = @{
    name = "Test Alliance"
    tag = "TEST"
    description = "A test alliance for automated testing"
} | ConvertTo-Json

try {
    $allianceResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/create" -Method Post -Body $allianceBody -ContentType "application/json" -Headers $headers1
    $allianceId = $allianceResponse.allianceId
    Write-Host "[OK] Alliance created: $allianceId (tag: TEST)" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to create alliance: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 5: List all alliances
Write-Host "[5/15] Listing all alliances..." -ForegroundColor Yellow
$alliancesList = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/list" -Headers $headers1
$allianceCount = $alliancesList.alliances.Count
Write-Host "[OK] Found $allianceCount alliance(s)" -ForegroundColor Green

# Step 6: Get alliance info
Write-Host "[6/15] Getting alliance info..." -ForegroundColor Yellow
$allianceInfo = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/info?allianceId=$allianceId" -Headers $headers1
$memberCount = $allianceInfo.alliance.members.Count
Write-Host "[OK] Alliance has $memberCount member(s)" -ForegroundColor Green

# Step 7: Player 2 applies to join the alliance
Write-Host "[7/15] Player 2 applying to join alliance..." -ForegroundColor Yellow
$joinBody = @{
    allianceId = $allianceId
} | ConvertTo-Json

try {
    $joinResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/join" -Method Post -Body $joinBody -ContentType "application/json" -Headers $headers2
    Write-Host "[OK] Application sent" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to apply: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 8: Player 1 invites Player 3
Write-Host "[8/15] Player 1 inviting Player 3..." -ForegroundColor Yellow
$inviteBody = @{
    targetPlayerId = $player3Id
} | ConvertTo-Json

try {
    $inviteResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/invite" -Method Post -Body $inviteBody -ContentType "application/json" -Headers $headers1
    Write-Host "[OK] Invitation sent to Player 3" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to invite: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 9: Player 3 lists their invitations
Write-Host "[9/15] Player 3 checking invitations..." -ForegroundColor Yellow
$invites = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/list-invites" -Headers $headers3
$inviteCount = $invites.invitations.Count
if ($inviteCount -gt 0) {
    $invitationId = $invites.invitations[0].invitation_id
    Write-Host "[OK] Player 3 has $inviteCount invitation(s)" -ForegroundColor Green
} else {
    Write-Host "[ERROR] No invitations found for Player 3" -ForegroundColor Red
    exit 1
}

# Step 10: Player 3 accepts the invitation
Write-Host "[10/15] Player 3 accepting invitation..." -ForegroundColor Yellow
$acceptBody = @{
    invitationId = $invitationId
} | ConvertTo-Json

try {
    $acceptResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/accept-invite" -Method Post -Body $acceptBody -ContentType "application/json" -Headers $headers3
    Write-Host "[OK] Player 3 joined alliance" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to accept invitation: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# Step 11: Check alliance members
Write-Host "[11/15] Checking alliance members..." -ForegroundColor Yellow
$allianceInfo = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/info?allianceId=$allianceId" -Headers $headers1
$memberCount = $allianceInfo.alliance.members.Count
if ($memberCount -eq 2) {
    Write-Host "[OK] Alliance now has $memberCount members" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Expected 2 members, found $memberCount" -ForegroundColor Red
    exit 1
}

# Step 12: Player 1 promotes Player 3 to officer
Write-Host "[12/15] Player 1 promoting Player 3 to officer..." -ForegroundColor Yellow
$promoteBody = @{
    targetPlayerId = $player3Id
    allianceId = $allianceId
} | ConvertTo-Json

try {
    $promoteResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/promote" -Method Post -Body $promoteBody -ContentType "application/json" -Headers $headers1
    Write-Host "[OK] Player 3 promoted to officer" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to promote: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 13: Player 1 demotes Player 3 back to member
Write-Host "[13/15] Player 1 demoting Player 3 back to member..." -ForegroundColor Yellow
$demoteBody = @{
    targetPlayerId = $player3Id
    allianceId = $allianceId
} | ConvertTo-Json

try {
    $demoteResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/demote" -Method Post -Body $demoteBody -ContentType "application/json" -Headers $headers1
    Write-Host "[OK] Player 3 demoted to member" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to demote: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 14: Player 3 leaves the alliance
Write-Host "[14/15] Player 3 leaving alliance..." -ForegroundColor Yellow
try {
    $leaveResponse = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/leave" -Method Post -Headers $headers3
    Write-Host "[OK] Player 3 left alliance" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to leave: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds 1

# Step 15: Verify final member count
Write-Host "[15/15] Verifying final member count..." -ForegroundColor Yellow
$allianceInfo = Invoke-RestMethod -Uri "$BaseUrl/api/alliance/info?allianceId=$allianceId" -Headers $headers1
$finalMemberCount = $allianceInfo.alliance.members.Count

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Alliance ID: $allianceId" -ForegroundColor White
Write-Host "Alliance Name: $($allianceInfo.alliance.name)" -ForegroundColor White
Write-Host "Alliance Tag: $($allianceInfo.alliance.tag)" -ForegroundColor White
Write-Host "Leader: $($allianceInfo.alliance.leaderId)" -ForegroundColor White
Write-Host "Final Member Count: $finalMemberCount" -ForegroundColor White
Write-Host ""
Write-Host "Members:" -ForegroundColor Yellow
foreach ($member in $allianceInfo.alliance.members) {
    Write-Host "  - $($member.playerName) ($($member.role))" -ForegroundColor White
}
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
