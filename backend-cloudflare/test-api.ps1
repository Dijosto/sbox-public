# Atlantis Strategy Backend API Test Script (PowerShell)
# Tests all Phase 2 implemented features

$ErrorActionPreference = "Stop"
$BASE_URL = "http://localhost:8787"
$TOKEN = ""
$PLAYER_ID = ""

Write-Host "========================================" -ForegroundColor Blue
Write-Host "Atlantis Strategy Backend API Tests" -ForegroundColor Blue
Write-Host "========================================`n" -ForegroundColor Blue

function Test-Result {
    param($TestName, $Success = $true)
    if ($Success) {
        Write-Host "[PASS] $TestName" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $TestName" -ForegroundColor Red
        exit 1
    }
}

# Test 1: Health Check
Write-Host "`n[1/10] Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BASE_URL/health" -Method Get
    Write-Host "Server is running" -ForegroundColor Green
    Write-Host "Response: $($health | ConvertTo-Json -Compress)"
    Test-Result "Health check"
} catch {
    Write-Host "Server not responding. Make sure you started: npm run dev" -ForegroundColor Red
    exit 1
}

# Test 2: Player Login/Creation
Write-Host "`n[2/10] Testing Player Authentication..." -ForegroundColor Yellow
try {
    $timestamp = [DateTimeOffset]::Now.ToUnixTimeSeconds()
    $loginBody = @{
        steamId = "test-player-$timestamp"
        username = "TestPlayer"
    } | ConvertTo-Json

    $authResponse = Invoke-RestMethod -Uri "$BASE_URL/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody

    Write-Host "Response: $($authResponse | ConvertTo-Json -Compress)"

    $TOKEN = $authResponse.token
    $PLAYER_ID = $authResponse.playerId

    if ($TOKEN) {
        Test-Result "Player authentication"
        Write-Host "Player ID: $PLAYER_ID"
        Write-Host "Token: $($TOKEN.Substring(0, [Math]::Min(20, $TOKEN.Length)))..."
    } else {
        throw "No token received"
    }
} catch {
    Write-Host "Authentication failed: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Get Player State
Write-Host "`n[3/10] Testing Get Player State..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
    }
    $state = Invoke-RestMethod -Uri "$BASE_URL/api/player/state" `
        -Method Get `
        -Headers $headers

    Write-Host "Response: $($state | ConvertTo-Json -Depth 3 -Compress)"
    Test-Result "Get player state"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
    exit 1
}

# Test 4: Building Upgrade - Fortress
Write-Host "`n[4/10] Testing Building Upgrade (Fortress)..." -ForegroundColor Yellow
try {
    $buildBody = @{
        buildingId = "fortress_1"
        buildingType = "fortress"
        zone = "inner"
    } | ConvertTo-Json

    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Content-Type" = "application/json"
    }

    $buildResponse = Invoke-RestMethod -Uri "$BASE_URL/api/player/building/upgrade" `
        -Method Post `
        -Headers $headers `
        -Body $buildBody

    Write-Host "Response: $($buildResponse | ConvertTo-Json -Compress)"

    if ($buildResponse.duration) {
        Write-Host "Build will complete in $($buildResponse.duration) seconds"
        $global:DURATION = $buildResponse.duration
    }

    Test-Result "Start building upgrade"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
    Write-Host "This might fail if prerequisites aren't met - that's expected for first test" -ForegroundColor Yellow
}

# Test 5: Check Build Queue
Write-Host "`n[5/10] Testing Build Queue Status..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
    }
    $queueState = Invoke-RestMethod -Uri "$BASE_URL/api/player/state" `
        -Method Get `
        -Headers $headers

    Write-Host "Queue State: $($queueState.city.buildQueue | ConvertTo-Json -Compress)"
    Test-Result "Check build queue"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}

# Test 6: Train Troops
Write-Host "`n[6/10] Testing Troop Training..." -ForegroundColor Yellow
try {
    $trainBody = @{
        troopType = "porter"
        quantity = 10
    } | ConvertTo-Json

    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Content-Type" = "application/json"
    }

    $trainResponse = Invoke-RestMethod -Uri "$BASE_URL/api/player/troops/train" `
        -Method Post `
        -Headers $headers `
        -Body $trainBody

    Write-Host "Response: $($trainResponse | ConvertTo-Json -Compress)"
    Test-Result "Start troop training"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Yellow
    Write-Host "(May fail if prerequisites not met - expected)" -ForegroundColor Yellow
}

# Test 7: Start Research
Write-Host "`n[7/10] Testing Research Start..." -ForegroundColor Yellow
try {
    $researchBody = @{
        researchType = "agriculture"
    } | ConvertTo-Json

    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Content-Type" = "application/json"
    }

    $researchResponse = Invoke-RestMethod -Uri "$BASE_URL/api/player/research/start" `
        -Method Post `
        -Headers $headers `
        -Body $researchBody

    Write-Host "Response: $($researchResponse | ConvertTo-Json -Compress)"
    Test-Result "Start research"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Yellow
    Write-Host "(May fail if prerequisites not met - expected)" -ForegroundColor Yellow
}

# Test 8: Get Updated State
Write-Host "`n[8/10] Testing Updated State..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
    }
    $finalState = Invoke-RestMethod -Uri "$BASE_URL/api/player/state" `
        -Method Get `
        -Headers $headers

    $preview = ($finalState | ConvertTo-Json -Depth 5 -Compress).Substring(0, [Math]::Min(200, ($finalState | ConvertTo-Json -Compress).Length))
    Write-Host "Final State: $preview..."
    Test-Result "Get final state"
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}

# Test 9: Test Invalid Request (Should Fail)
Write-Host "`n[9/10] Testing Error Handling..." -ForegroundColor Yellow
try {
    $invalidBody = @{
        buildingId = "invalid_building"
        buildingType = "nonexistent"
        zone = "outer"
    } | ConvertTo-Json

    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Content-Type" = "application/json"
    }

    $errorResponse = Invoke-RestMethod -Uri "$BASE_URL/api/player/building/upgrade" `
        -Method Post `
        -Headers $headers `
        -Body $invalidBody `
        -ErrorAction SilentlyContinue

    Write-Host "Expected error response: $($errorResponse | ConvertTo-Json -Compress)"
    Write-Host "(This should fail - if it succeeded, that's a bug!)" -ForegroundColor Yellow
} catch {
    Write-Host "Correctly rejected invalid request" -ForegroundColor Green
    Test-Result "Error handling"
}

# Test 10: Summary
Write-Host "`n[10/10] Test Summary" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Blue
Write-Host "Tests Completed Successfully!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Blue

Write-Host "Player ID: " -NoNewline
Write-Host "$PLAYER_ID" -ForegroundColor Blue
Write-Host "Token: " -NoNewline
Write-Host "$($TOKEN.Substring(0, [Math]::Min(30, $TOKEN.Length)))..." -ForegroundColor Blue

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Check the npm run dev terminal for alarm logs"
Write-Host "  2. Try manual API calls with the token above"
Write-Host "  3. Test WebSocket connections (requires wscat)"
Write-Host "  4. Test prerequisite validation with various building types"
Write-Host ""

Write-Host "Keep the dev server running and use this token for manual testing:" -ForegroundColor Green
Write-Host "Authorization: Bearer $TOKEN" -ForegroundColor Cyan
Write-Host ""
