# Testing Guide - Atlantis Strategy Backend

This guide walks you through setting up and testing the Cloudflare Workers backend locally.

## Prerequisites

- Node.js 18+ installed
- npm or yarn package manager
- A Cloudflare account (free tier works)

## Step 1: Install Dependencies

```bash
cd backend-cloudflare
npm install
```

This installs:
- `wrangler` - Cloudflare Workers CLI
- `typescript` - TypeScript compiler
- `@cloudflare/workers-types` - Type definitions

## Step 2: Create D1 Database (Local Development)

For local testing, Wrangler creates a local SQLite database automatically.

```bash
# Create local D1 database
wrangler d1 create atlantis-strategy-db --local

# Initialize schema (run migrations)
wrangler d1 execute atlantis-strategy-db --local --file=./schema.sql
```

**Note:** The `--local` flag means this runs on your machine, not in Cloudflare.

## Step 3: Create KV Namespace (Local Development)

For local testing, KV is simulated automatically by Wrangler.

```bash
# Create local KV namespace
wrangler kv:namespace create GAME_CONFIG --local
```

## Step 4: Configure wrangler.toml for Local Testing

The `wrangler.toml` is already configured for local development. For local testing, you can use placeholder IDs:

```toml
# For local development, these IDs don't need to be real
database_id = "local-db"
id = "local-kv"
```

## Step 5: Start Development Server

```bash
npm run dev
```

This starts the Wrangler dev server on `http://localhost:8787`

You should see output like:
```
⎔ Starting local server...
⎔ Ready on http://localhost:8787
```

## Step 6: Test the API

### Method 1: Using curl (Terminal)

Open a new terminal window and run:

```bash
# Test health endpoint
curl http://localhost:8787/health

# Test player creation (mock auth)
curl -X POST http://localhost:8787/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"steamId": "test-player-123", "username": "TestPlayer"}'

# Get player state
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN_FROM_LOGIN"

# Start building upgrade
curl -X POST http://localhost:8787/api/player/building/upgrade \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "buildingId": "fortress_1",
    "buildingType": "fortress",
    "zone": "inner"
  }'

# Check build queue
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Method 2: Using the Test Script (Automated)

We'll create a test script for you in the next step.

### Method 3: Using Postman/Insomnia

1. Import the endpoints into Postman
2. Set base URL: `http://localhost:8787`
3. Test each endpoint manually

## Step 7: Test Durable Objects Alarms

To test the timer system:

1. **Start a building upgrade** (should take 45+ seconds based on config)
2. **Wait for completion** - Durable Object alarm will fire automatically
3. **Check logs** in the Wrangler terminal for:
   ```
   [PlayerDO] Alarm fired, processing completions
   [PlayerDO] Completed build: fortress_1 to level 2
   ```

4. **Query player state** again to verify building level increased

## Step 8: Test WebSocket Notifications

```bash
# Connect via wscat (install with: npm install -g wscat)
wscat -c ws://localhost:8787/ws
```

Then in another terminal, trigger events:
```bash
curl -X POST http://localhost:8787/api/player/building/upgrade \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"buildingId": "farm_1", "buildingType": "farm", "zone": "outer"}'
```

You should receive WebSocket messages when the building completes.

## Common Issues

### Issue 1: "Error: No such module"
**Solution:** Make sure you ran `npm install` and TypeScript compilation succeeded.

### Issue 2: "Durable Object binding not found"
**Solution:** This is expected for some DOs not yet implemented. PlayerDO should work.

### Issue 3: "Database not found"
**Solution:** Run `wrangler d1 execute atlantis-strategy-db --local --file=./schema.sql`

### Issue 4: "Workers not responding"
**Solution:**
- Check the terminal for errors
- Make sure no other process is using port 8787
- Try `npm run dev` again

## What to Test

### Phase 2 Features (All Implemented)

1. **Building System**
   - Start building upgrade
   - Check prerequisites validation
   - Verify resource deduction
   - Wait for timer completion (alarm)
   - Verify building level increased

2. **Troop Training**
   - Start troop training
   - Check garrison speed calculation
   - Cancel training (50% refund)
   - Wait for completion
   - Verify troops added to army

3. **Research System**
   - Start research
   - Check prerequisite validation
   - Verify single-queue limitation
   - Wait for completion
   - Verify research level increased

4. **Resource Production**
   - Login to trigger resource calculation
   - Verify production rates calculated correctly
   - Check storage caps applied
   - Verify research bonuses included

5. **Queue Management**
   - Test worker limits (building queue)
   - Test unlimited training queues
   - Test single research queue
   - Test cancellation with refunds

## Next Steps

After basic testing works:

1. **Load Testing** - Test with multiple concurrent requests
2. **Alarm Testing** - Verify timers complete at correct times
3. **WebSocket Testing** - Verify real-time notifications work
4. **Persistence Testing** - Restart server, verify state persists
5. **Combat Testing** - Will be implemented in Phase 3

## Development Workflow

```bash
# 1. Start dev server
npm run dev

# 2. Make code changes
# (Server auto-reloads on file changes)

# 3. Test changes
curl http://localhost:8787/api/...

# 4. Check logs in terminal

# 5. Repeat
```

## Deployment to Cloudflare (Later)

When ready to deploy to production:

```bash
# 1. Create real D1 database
wrangler d1 create atlantis-strategy-db

# 2. Update wrangler.toml with real database_id

# 3. Run migrations
wrangler d1 execute atlantis-strategy-db --file=./schema.sql

# 4. Create real KV namespace
wrangler kv:namespace create GAME_CONFIG

# 5. Update wrangler.toml with real KV id

# 6. Deploy
npm run deploy
```

## Troubleshooting Commands

```bash
# View Wrangler version
wrangler --version

# View logs (if deployed)
npm run tail

# Type check TypeScript
npx tsc --noEmit

# Clear Wrangler cache
rm -rf .wrangler
```

## Getting Help

- Check Wrangler logs in terminal
- Review Cloudflare Workers docs: https://developers.cloudflare.com/workers/
- Check Durable Objects docs: https://developers.cloudflare.com/durable-objects/
