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
Write-Host "[1/13] Checking server health..." -ForegroundColor Yellow
$health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get
$health | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 2: Create Player 1
Write-Host "[2/13] Creating Player 1 (Attacker)..." -ForegroundColor Yellow
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
Write-Host "[3/13] Creating Player 2 (Defender)..." -ForegroundColor Yellow
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
Write-Host "[4/13] Getting Player 1 state (city location)..." -ForegroundColor Yellow
$headers1 = @{ Authorization = "Bearer $player1Token" }
$player1State = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
$player1State.city.position | ConvertTo-Json
$player1X = $player1State.city.position.x
$player1Y = $player1State.city.position.y
Write-Host "Player 1 City: ($player1X, $player1Y)" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 5: Get Player 2 State
Write-Host "[5/13] Getting Player 2 state (city location)..." -ForegroundColor Yellow
$headers2 = @{ Authorization = "Bearer $player2Token" }
$player2State = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers2
$player2State.city.position | ConvertTo-Json
$player2X = $player2State.city.position.x
$player2Y = $player2State.city.position.y
Write-Host "Player 2 City: ($player2X, $player2Y)" -ForegroundColor Green
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 6: Query world tiles
Write-Host "[6/13] Querying world map (Player 1's region)..." -ForegroundColor Yellow
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
Write-Host "[7/13] Getting tile details for Player 2's city..." -ForegroundColor Yellow
$tile = Invoke-RestMethod -Uri "$BaseUrl/api/world/tile/$player2X/$player2Y" -Method Get
$tile | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 8: Search players
Write-Host "[8/13] Searching for players..." -ForegroundColor Yellow
$search = Invoke-RestMethod -Uri "$BaseUrl/api/world/search?playerName=Player" -Method Get
$search | ConvertTo-Json
Write-Host ""
Start-Sleep -Milliseconds 200

# Step 9: Build garrisons for both players
Write-Host "[9/13] Building garrisons (required for training)..." -ForegroundColor Yellow

Write-Host "Building garrison for Player 2 (Defender)..." -ForegroundColor Blue
$garrisonBody2 = @{
    buildingId = "garrison_1"
    buildingType = "garrison"
    zone = "inner"
} | ConvertTo-Json

$garrison2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/building/upgrade" -Method Post -Body $garrisonBody2 -ContentType "application/json" -Headers $headers2
Write-Host "Garrison 2 build started, duration: $($garrison2.duration)s" -ForegroundColor Green
$garrison2CompletionTime = $garrison2.completionTime
Start-Sleep -Milliseconds 500

Write-Host "Building garrison for Player 1 (Attacker)..." -ForegroundColor Blue
$garrisonBody1 = @{
    buildingId = "garrison_1"
    buildingType = "garrison"
    zone = "inner"
} | ConvertTo-Json

$garrison1 = Invoke-RestMethod -Uri "$BaseUrl/api/player/building/upgrade" -Method Post -Body $garrisonBody1 -ContentType "application/json" -Headers $headers1
Write-Host "Garrison 1 build started, duration: $($garrison1.duration)s" -ForegroundColor Green
$garrison1CompletionTime = $garrison1.completionTime
Write-Host ""

# Calculate exact wait time based on completion timestamps
$now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$maxCompletionTime = [Math]::Max($garrison1CompletionTime, $garrison2CompletionTime)
$waitMs = $maxCompletionTime - $now + 2000  # +2 second buffer for alarm processing
$waitSeconds = [Math]::Max(0, [Math]::Ceiling($waitMs / 1000))

Write-Host "Waiting $waitSeconds seconds for garrison construction to complete..." -ForegroundColor Blue
Start-Sleep -Seconds $waitSeconds

# Verify garrisons are built (with retries for alarm processing)
Write-Host "Verifying garrisons are ready..." -ForegroundColor Blue
$garrisonsReady = $false
$garrison1Check = $null
$garrison2Check = $null

for ($i = 0; $i -lt 3; $i++) {
    $verifyState1 = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
    $garrison1Check = $verifyState1.city.innerCity.garrison_1

    $verifyState2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers2
    $garrison2Check = $verifyState2.city.innerCity.garrison_1

    if ($garrison1Check -and $garrison2Check) {
        Write-Host "[OK] Garrisons built! P1: Level $($garrison1Check.level), P2: Level $($garrison2Check.level)" -ForegroundColor Green
        $garrisonsReady = $true
        break
    } else {
        if ($i -lt 2) {
            Write-Host "[WAIT] Garrisons not ready yet, retrying ($($i+1)/3)..." -ForegroundColor Yellow
            Start-Sleep -Seconds 1
        }
    }
}

if (-not $garrisonsReady) {
    Write-Host "[ERROR] Garrisons not ready after waiting for completion time, aborting test" -ForegroundColor Red
    Write-Host "P1 garrison: $($garrison1Check | ConvertTo-Json)" -ForegroundColor Red
    Write-Host "P2 garrison: $($garrison2Check | ConvertTo-Json)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 10: Train troops for Player 2
Write-Host "[10/13] Training troops for Player 2 (Defender)..." -ForegroundColor Yellow
$trainBody2 = @{
    troopType = "conscript"
    quantity = 100
} | ConvertTo-Json

$train2 = Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/train" -Method Post -Body $trainBody2 -ContentType "application/json" -Headers $headers2
$train2 | ConvertTo-Json
$train2CompletionTime = $train2.completionTime
Write-Host "Training started, completion time: $train2CompletionTime" -ForegroundColor Green

# Complete training
try {
    Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/complete" -Method Post -Body "{}" -ContentType "application/json" -Headers $headers2 | Out-Null
} catch {}
Write-Host ""
Write-Host "Waiting for Durable Object to settle..." -ForegroundColor Blue
Start-Sleep -Seconds 1

# Step 11: Train troops for Player 1 (with retry for Wrangler race condition)
Write-Host "[11/13] Training troops for Player 1 (Attacker)..." -ForegroundColor Yellow
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
$train1CompletionTime = $train1.completionTime
Write-Host "Training started, completion time: $train1CompletionTime" -ForegroundColor Green

# Complete training
try {
    Invoke-RestMethod -Uri "$BaseUrl/api/player/troops/complete" -Method Post -Body "{}" -ContentType "application/json" -Headers $headers1 | Out-Null
} catch {}
Write-Host ""

# Calculate exact wait time based on completion timestamps
$now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$maxTrainingTime = [Math]::Max($train1CompletionTime, $train2CompletionTime)
$waitMs = $maxTrainingTime - $now + 2000  # +2 second buffer for alarm processing
$waitSeconds = [Math]::Max(0, [Math]::Ceiling($waitMs / 1000))

Write-Host "Waiting $waitSeconds seconds for troop training to complete..." -ForegroundColor Blue
Start-Sleep -Seconds $waitSeconds

# Verify troops are ready (with retries for alarm processing)
Write-Host "Verifying troops are ready..." -ForegroundColor Blue
$troopsReady = $false
$conscripts = $null

for ($i = 0; $i -lt 3; $i++) {
    $verifyState = Invoke-RestMethod -Uri "$BaseUrl/api/player/state" -Method Get -Headers $headers1
    $conscripts = ($verifyState.troops | Where-Object { $_.troopType -eq 'conscript' } | Select-Object -First 1)

    if ($conscripts -and $conscripts.quantity -ge 100) {
        Write-Host "[OK] Troops verified: $($conscripts.quantity) conscripts available" -ForegroundColor Green
        $troopsReady = $true
        break
    } else {
        if ($i -lt 2) {
            Write-Host "[WAIT] Troops not ready yet, retrying ($($i+1)/3)..." -ForegroundColor Yellow
            Start-Sleep -Seconds 1
        }
    }
}

if (-not $troopsReady) {
    Write-Host "[ERROR] Troops not ready after waiting for completion time, aborting test" -ForegroundColor Red
    Write-Host "Conscripts: $($conscripts | ConvertTo-Json)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 12: Send PvP march (with retry for Wrangler race condition)
Write-Host "[12/13] Sending PvP march (Player 1 → Player 2)..." -ForegroundColor Yellow
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

# Step 13: Check results
Write-Host "[13/13] Checking battle results..." -ForegroundColor Yellow
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
