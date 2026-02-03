# Comprehensive Test Script for World Map & PvP System
# Tests: World generation, player spawning, map queries, NPC combat, PvP combat

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:8787"

# Generate unique player IDs for each test run
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$player1SteamId = "test_player_$timestamp"
$player2SteamId = "test_player_$($timestamp + 1)"

Write-Host "========================================" -ForegroundColor Blue
Write-Host "World Map & PvP Combat Test Suite" -ForegroundColor Blue
Write-Host "========================================" -ForegroundColor Blue
Write-Host ""

# Step 1: Health Check
Write-Host "[1/12] Checking server health..." -ForegroundColor Yellow
$health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get
$health | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 2: Create Player 1
Write-Host "[2/12] Creating Player 1 (Attacker)..." -ForegroundColor Yellow
$player1Body = @{
    steamId = $player1SteamId
    username = "Attacker"
} | ConvertTo-Json

$player1 = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $player1Body -ContentType "application/json"
$player1 | ConvertTo-Json
$player1Token = $player1.token
$player1Id = $player1.playerId
Write-Host "Player 1 Token: $player1Token" -ForegroundColor Green
Write-Host "Player 1 ID: $player1Id" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 3: Create Player 2
Write-Host "[3/12] Creating Player 2 (Defender)..." -ForegroundColor Yellow
$player2Body = @{
    steamId = $player2SteamId
    username = "Defender"
} | ConvertTo-Json

$player2 = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $player2Body -ContentType "application/json"
$player2 | ConvertTo-Json
$player2Token = $player2.token
$player2Id = $player2.playerId
Write-Host "Player 2 Token: $player2Token" -ForegroundColor Green
Write-Host "Player 2 ID: $player2Id" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 4: Get Player 1 State
Write-Host "[4/12] Getting Player 1 state (city location)..." -ForegroundColor Yellow
$headers1 = @{ Authorization = "Bearer $player1Token" }
$player1State = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
$player1State.city.position | ConvertTo-Json
$player1X = $player1State.city.position.x
$player1Y = $player1State.city.position.y
Write-Host "Player 1 City: ($player1X, $player1Y)" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 5: Get Player 2 State
Write-Host "[5/12] Getting Player 2 state (city location)..." -ForegroundColor Yellow
$headers2 = @{ Authorization = "Bearer $player2Token" }
$player2State = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers2
$player2State.city.position | ConvertTo-Json
$player2X = $player2State.city.position.x
$player2Y = $player2State.city.position.y
Write-Host "Player 2 City: ($player2X, $player2Y)" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 6: Query world tiles
Write-Host "[6/12] Querying world map (Player 1's region)..." -ForegroundColor Yellow
$regionX = [Math]::Floor($player1X / 100)
$regionY = [Math]::Floor($player1Y / 100)
Write-Host "Region: ($regionX, $regionY)" -ForegroundColor Blue

$tiles = Invoke-RestMethod -Uri "$BaseUrl/api/world/tiles?regionX=$regionX&regionY=$regionY" -Method Get
$summary = @{
    region = $tiles.region
    tileCount = $tiles.count
    cities = $tiles.tiles | Where-Object { $_.tileType -eq 'city' }
    npcCamps = ($tiles.tiles | Where-Object { $_.tileType -eq 'npc_camp' } | Select-Object x, y, level -First 3)
    wilderness = ($tiles.tiles | Where-Object { $_.tileType -eq 'wilderness' } | Select-Object x, y, resourceType, level -First 3)
}
$summary | ConvertTo-Json -Depth 3
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 7: Get specific tile
Write-Host "[7/12] Getting tile details for Player 2's city..." -ForegroundColor Yellow
$tile = Invoke-RestMethod -Uri "$BaseUrl/api/world/tile/$player2X/$player2Y" -Method Get
$tile | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 8: Search players
Write-Host "[8/12] Searching for players..." -ForegroundColor Yellow
$search = Invoke-RestMethod -Uri "$BaseUrl/api/world/search?playerName=Player" -Method Get
$search | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 9: Train troops for Player 2
Write-Host "[9/12] Training troops for Player 2 (Defender)..." -ForegroundColor Yellow
$trainBody2 = @{
    troopType = "conscript"
    quantity = 100
} | ConvertTo-Json

$train2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/train" -Method Post -Body $trainBody2 -ContentType "application/json" -Headers $headers2
$train2 | ConvertTo-Json

# Complete training
try {
    Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/complete" -Method Post -Body "{}" -ContentType "application/json" -Headers $headers2 | Out-Null
} catch {}
Write-Host ""
Write-Host "Waiting for Durable Object to settle..." -ForegroundColor Blue
Start-Sleep -Seconds 1

# Step 10: Train troops for Player 1 (with retry for Wrangler race condition)
Write-Host "[10/12] Training troops for Player 1 (Attacker)..." -ForegroundColor Yellow
$trainBody1 = @{
    troopType = "conscript"
    quantity = 150
} | ConvertTo-Json

$train1 = $null
$retries = 3
for ($i = 0; $i -lt $retries; $i++) {
    try {
        $train1 = Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/train" -Method Post -Body $trainBody1 -ContentType "application/json" -Headers $headers1
        break
    } catch {
        if ($i -lt $retries - 1) {
            Write-Host "Worker restarted, retrying ($($i+1)/$retries)..." -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        } else {
            throw
        }
    }
}

$train1 | ConvertTo-Json

# Complete training
try {
    Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/complete" -Method Post -Body "{}" -ContentType "application/json" -Headers $headers1 | Out-Null
} catch {}
Write-Host ""

# Wait for state to persist
Write-Host "Waiting for Durable Object to persist state..." -ForegroundColor Blue
Start-Sleep -Seconds 3

# Verify troops are available before march (with retries for alarm processing)
Write-Host "Verifying troops are ready..." -ForegroundColor Blue
$troopsReady = $false
for ($i = 0; $i -lt 5; $i++) {
    $verifyState = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
    $conscripts = ($verifyState.troops | Where-Object { $_.troopType -eq 'conscript' } | Select-Object -First 1)
    if ($conscripts -and $conscripts.quantity -ge 100) {
        Write-Host "[OK] Troops verified: $($conscripts.quantity) conscripts available" -ForegroundColor Green
        $troopsReady = $true
        break
    } else {
        Write-Host "[WAIT] Waiting for training alarm to fire... ($($i+1)/5)" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

if (-not $troopsReady) {
    Write-Host "[WARN] Troops may not be ready yet, attempting march anyway..." -ForegroundColor Yellow
}
Write-Host ""

# Step 11: Send PvP march (with retry for Wrangler race condition)
Write-Host "[11/12] Sending PvP march (Player 1 → Player 2)..." -ForegroundColor Yellow
Write-Host "Attacker: ($player1X, $player1Y) → Defender: ($player2X, $player2Y)" -ForegroundColor Blue

$marchBody = @{
    destination = @{ x = $player2X; y = $player2Y }
    troops = @(
        @{ troopType = "conscript"; quantity = 100 }
    )
    marchType = "attack"
    targetType = "player"
} | ConvertTo-Json -Depth 3

$march = $null
$retries = 3
for ($i = 0; $i -lt $retries; $i++) {
    try {
        $march = Invoke-RestMethod -Uri "$BaseUrl/api/player/march/send" -Method Post -Body $marchBody -ContentType "application/json" -Headers $headers1
        break
    } catch {
        if ($i -lt $retries - 1) {
            Write-Host "Worker restarted, retrying march ($($i+1)/$retries)..." -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        } else {
            throw
        }
    }
}

$march | ConvertTo-Json
$marchId = $march.marchId

if (-not $marchId) {
    Write-Host "Failed to send march!" -ForegroundColor Red
    exit 1
}

Write-Host "March ID: $marchId" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Wait for march
$travelTime = if ($march.travelTime) { $march.travelTime } else { 10 }
Write-Host "Waiting ${travelTime}s for march to arrive..." -ForegroundColor Blue
Start-Sleep -Seconds ($travelTime + 2)

# Step 12: Check results
Write-Host "[12/12] Checking battle results..." -ForegroundColor Yellow
Start-Sleep -Milliseconds 200

Write-Host "Player 1 (Attacker) Messages:" -ForegroundColor Blue
$messages1 = Invoke-RestMethod -Uri "$BaseUrl/api/player/messages" -Method Get -Headers $headers1
$messages1.messages | Where-Object { $_.message_type -eq 'battle_report' } | Select-Object subject, body, metadata | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

Write-Host "Player 2 (Defender) Messages:" -ForegroundColor Blue
$messages2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/messages" -Method Get -Headers $headers2
$messages2.messages | Where-Object { $_.message_type -eq 'battle_report' } | Select-Object subject, body, metadata | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

Write-Host "Player 1 Final State:" -ForegroundColor Blue
$final1 = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
@{
    resources = $final1.resources
    troops = $final1.troops
    activeMarchesCount = $final1.activeMarches.Length
} | ConvertTo-Json
Write-Host ""

Write-Host "Player 2 Final State:" -ForegroundColor Blue
$final2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers2
@{
    resources = $final2.resources
    troops = $final2.troops
    activeMarchesCount = $final2.activeMarches.Length
} | ConvertTo-Json
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "Test Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
