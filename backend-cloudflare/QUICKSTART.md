# Quick Start - 5 Minute Setup

Get the backend running and tested in 5 minutes.

## Step 1: Install Dependencies (1 minute)

```bash
cd backend-cloudflare
npm install
```

Wait for installation to complete.

## Step 2: Start Development Server (30 seconds)

```bash
npm run dev
```

You should see:
```
⎔ Starting local server...
⎔ Ready on http://localhost:8787
```

**Leave this terminal window open!** The server is now running.

## Step 3: Run Automated Tests (2 minutes)

Open a **NEW terminal window** and run:

```bash
cd backend-cloudflare
./test-api.sh
```

This will:
- Test all API endpoints
- Create a test player
- Start building upgrades
- Train troops
- Start research
- Test queue systems
- Wait for building completion (if < 60s)

## What You Should See

### In Terminal 1 (Wrangler Dev Server):

```
⎔ Starting local server...
[wrangler] Ready on http://localhost:8787
[PlayerDO] Creating new player: test-player-123
[PlayerDO] Building upgrade started: fortress level 1 → 2
[PlayerDO] Scheduled alarm for 2025-02-03T12:34:56.789Z
[PlayerDO] Alarm fired, processing completions
[PlayerDO] Completed build: fortress_1 to level 2
```

### In Terminal 2 (Test Script):

```
========================================
Atlantis Strategy Backend API Tests
========================================

[1/10] Testing Health Endpoint...
✓ Server is running

[2/10] Testing Player Authentication...
✓ PASS: Player authentication

[3/10] Testing Get Player State...
✓ PASS: Get player state

[4/10] Testing Building Upgrade (Fortress)...
✓ PASS: Start building upgrade
Build will complete in 45 seconds

...

All Tests Completed!
```

## Next: Manual Testing

Once automated tests pass, try manual API calls:

```bash
# Health check
curl http://localhost:8787/health

# Create player (save the token!)
curl -X POST http://localhost:8787/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"steamId": "manual-test-123", "username": "ManualTester"}'

# Get player state (replace YOUR_TOKEN)
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN"

# Start building upgrade
curl -X POST http://localhost:8787/api/player/building/upgrade \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "buildingId": "farm_1",
    "buildingType": "farm",
    "zone": "outer"
  }'
```

## Testing Specific Features

### Test Building Upgrades

```bash
# Upgrade fortress (45 second build time)
curl -X POST http://localhost:8787/api/player/building/upgrade \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"buildingId": "fortress_1", "buildingType": "fortress", "zone": "inner"}'

# Check queue
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN" | grep buildQueue

# Wait 45+ seconds, check again (queue should be empty)
sleep 50
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN" | grep buildQueue
```

### Test Troop Training

```bash
# Train 10 porters
curl -X POST http://localhost:8787/api/player/troops/train \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"troopType": "porter", "quantity": 10}'

# Check training queue
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN" | grep trainQueue
```

### Test Research

```bash
# Start agriculture research
curl -X POST http://localhost:8787/api/player/research/start \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"researchType": "agriculture"}'

# Check research queue (single item only)
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN" | grep researchQueue

# Try starting another research (should fail - only 1 at a time)
curl -X POST http://localhost:8787/api/player/research/start \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"researchType": "metallurgy"}'
```

### Test WebSocket Notifications

Install wscat:
```bash
npm install -g wscat
```

Connect to WebSocket:
```bash
wscat -c ws://localhost:8787/ws
```

In another terminal, trigger events (building upgrade, etc). You'll see notifications in wscat window when events complete.

## Troubleshooting

### "Cannot find module"
```bash
# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
```

### "Port already in use"
```bash
# Kill existing process
killall wrangler
# Or use different port
wrangler dev --port 8788
```

### "Permission denied: ./test-api.sh"
```bash
chmod +x test-api.sh
```

### "curl: command not found"
- **Windows**: Use Git Bash or WSL
- **Mac/Linux**: Install curl via package manager

## What's Working Now (Phase 2 Complete)

- ✅ Player authentication and state management
- ✅ Building upgrade system with queues
- ✅ Worker limits (based on Fortress level)
- ✅ Troop training with garrison speed bonuses
- ✅ Research system with prerequisites
- ✅ Resource production calculations
- ✅ Durable Object alarms (automatic timer completion)
- ✅ WebSocket real-time notifications
- ✅ 50% refunds on queue cancellations

## What's Next (Phase 3)

- March system (send troops to attack/gather)
- Combat resolution
- Battle replays
- NPC camps
- Loot system

## Full Documentation

For detailed testing guide, see: [TESTING_GUIDE.md](./TESTING_GUIDE.md)
