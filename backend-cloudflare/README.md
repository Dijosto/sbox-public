# Atlantis Strategy - Cloudflare Workers Backend

Server-authoritative backend for the Atlantis Strategy multiplayer game, built on Cloudflare's edge infrastructure.

## Architecture

This backend implements a **zero-trust client model** where:
- All game logic runs server-side
- All validation happens server-side
- All RNG is seeded server-side
- Clients send intents, server validates and executes
- Combat outcomes are deterministic and server-computed

### Stack Components

- **Workers**: API endpoints for game actions
- **Durable Objects**: Per-player/alliance/region persistent state
- **D1 Database**: Relational data (accounts, leaderboards, audit logs)
- **KV Store**: Static game config (troop stats, building costs)
- **Queues**: Async processing (combat results, construction completion)
- **WebSockets**: Real-time notifications via Durable Objects

## Project Structure

```
backend-cloudflare/
├── src/
│   ├── workers/          # Worker entry points
│   │   ├── index.ts      # Main router
│   │   ├── auth.ts       # Authentication handler
│   │   ├── player.ts     # Player state handler
│   │   ├── building.ts   # Building actions handler
│   │   └── world.ts      # World map handler
│   ├── durable-objects/  # Durable Object implementations
│   │   ├── PlayerDO.ts   # Player state management
│   │   ├── AllianceDO.ts # Alliance management
│   │   ├── WorldRegionDO.ts # World map regions
│   │   └── CombatDO.ts   # Combat resolution
│   └── utils/            # Game configuration & formulas
│       ├── buildings.ts  # Building definitions (12+ types, 35 levels)
│       ├── troops.ts     # Troop stats & combat calculations (11+ types)
│       ├── research.ts   # Research tree & bonuses (13+ technologies)
│       └── resources.ts  # Resource production & economy formulas
├── schema.sql            # D1 database schema
├── wrangler.toml         # Cloudflare configuration
├── game-data.md          # Complete Dragons of Atlantis mechanics reference
├── package.json          # Dependencies
└── tsconfig.json         # TypeScript configuration
```

## Setup

### Prerequisites

- Node.js 18+ and npm
- Cloudflare account (free tier works)
- Wrangler CLI: `npm install -g wrangler`

### Initial Setup

1. **Clone and install dependencies:**
   ```bash
   cd backend-cloudflare
   npm install
   ```

2. **Login to Cloudflare:**
   ```bash
   wrangler login
   ```

3. **Create D1 Database:**
   ```bash
   wrangler d1 create atlantis-strategy-db
   ```

   Copy the database ID and update `wrangler.toml`:
   ```toml
   [[d1_databases]]
   binding = "DB"
   database_name = "atlantis-strategy-db"
   database_id = "YOUR_DATABASE_ID_HERE"
   ```

4. **Initialize database schema:**
   ```bash
   wrangler d1 execute atlantis-strategy-db --file=./schema.sql
   ```

5. **Create KV namespace:**
   ```bash
   wrangler kv:namespace create GAME_CONFIG
   ```

   Update `wrangler.toml` with the KV namespace ID.

6. **Create Queue:**
   ```bash
   wrangler queues create atlantis-game-queue
   ```

7. **Set environment secrets:**
   ```bash
   wrangler secret put JWT_SECRET
   # Enter a secure random string

   wrangler secret put STEAM_API_KEY
   # Enter your Steam Web API key from https://steamcommunity.com/dev/apikey
   ```

### Development

**Run local dev server:**
```bash
npm run dev
```

This starts a local Cloudflare Workers environment with hot reload on `http://localhost:8787`.

**Test endpoints:**
```bash
# Health check
curl http://localhost:8787/health

# Authenticate (mock)
curl -X POST http://localhost:8787/api/auth/steam \
  -H "Content-Type: application/json" \
  -d '{"steamId":"76561197960287930","clientVersion":"1.0.0","timestamp":1234567890}'

# Get player state (requires JWT from auth)
curl http://localhost:8787/api/player/state \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Deployment

**Deploy to Cloudflare:**
```bash
npm run deploy
```

**Deploy to production:**
```bash
wrangler deploy --env production
```

**View logs:**
```bash
npm run tail
```

## API Endpoints

### Authentication

#### `POST /api/auth/steam`
Authenticate with Steam ticket.

**Request:**
```json
{
  "steamId": "76561197960287930",
  "clientVersion": "1.0.0",
  "timestamp": 1234567890
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGc...",
  "playerId": "uuid",
  "playerName": "Player_76561197"
}
```

### Player State

#### `GET /api/player/state`
Get complete player state.

**Headers:**
```
Authorization: Bearer <jwt_token>
```

**Response:**
```json
{
  "playerId": "uuid",
  "playerName": "Player Name",
  "city": {
    "position": { "x": 500, "y": 500 },
    "innerCity": {
      "town_hall": { "buildingType": "town_hall", "level": 1 }
    },
    "outerFields": {},
    "buildQueue": [],
    "trainQueue": []
  },
  "resources": {
    "food": 1000,
    "wood": 1000,
    "stone": 500,
    "metal": 200,
    "gold": 100,
    "foodRate": 100,
    "woodRate": 100
  },
  "troops": [],
  "activeMarches": [],
  "lastUpdateTimestamp": 1234567890
}
```

### World Map

#### `GET /api/world/viewport`
Get tiles in viewport.

**Query Params:**
- `minX`, `maxX`, `minY`, `maxY`: Viewport bounds

**Response:**
```json
{
  "viewportMin": { "x": 450, "y": 450 },
  "viewportMax": { "x": 550, "y": 550 },
  "tiles": [
    {
      "position": { "x": 500, "y": 500 },
      "tileType": "city",
      "ownerId": "uuid",
      "ownerName": "Player",
      "level": 5
    }
  ],
  "marches": []
}
```

#### `GET /api/world/tile`
Get specific tile details.

**Query Params:**
- `x`, `y`: Tile coordinates

### WebSocket

#### `GET /ws?token=<jwt>`
Upgrade to WebSocket for real-time notifications.

**Events:**
```json
{
  "type": "attack_incoming",
  "timestamp": 1234567890,
  "data": "{\"attackerId\":\"uuid\",\"arrivalTime\":1234567890}"
}
```

## Durable Objects

### PlayerDurableObject

One instance per player. Manages:
- City state (buildings, queues)
- Resources (with lazy calculation)
- Troops and marches
- Research progress
- WebSocket connection for push notifications

**Key Methods:**
- `loadState()`: Load persisted state from storage
- `saveState()`: Persist state transactionally
- `updateResources()`: Calculate accumulated resources
- `pushEvent()`: Send WebSocket notification

### WorldRegionDurableObject

One instance per 100×100 tile region. Manages:
- Tile ownership and states
- Active marches in region
- Combat scheduling via alarms

**Key Methods:**
- `alarm()`: Triggered when march arrives, initiates combat

### CombatDurableObject

Ephemeral instance per combat. Manages:
- Deterministic combat resolution
- Combat log generation
- Result distribution via queue

### AllianceDurableObject

One instance per alliance. Manages:
- Member list
- Alliance chat (real-time via WebSocket)
- Coordinated attacks

## Database Schema

### Key Tables

- **players**: Player accounts and Steam linkage
- **world_tiles**: Persistent world map state
- **command_log**: Full audit trail of player actions
- **battle_reports**: Combat history for replay
- **state_snapshots**: Periodic state backups for recovery
- **leaderboard**: Rankings by various categories

See `schema.sql` for complete schema.

## Anti-Cheat Measures

✅ **Implemented:**
- All game logic server-side
- Server-side resource calculation (client never calculates)
- Server-side combat resolution with deterministic RNG
- JWT authentication on all endpoints
- Command logging for audit trail

🚧 **TODO:**
- Rate limiting per player (currently basic)
- State hashing verification
- Replay system from command logs
- Automated anomaly detection

## Performance Considerations

### Durable Objects Billing
- Charged per active duration
- WebSocket connections keep DO active → cost more
- Use hibernation for inactive players
- Batch operations where possible

### D1 Limits
- 100K rows read/day (free tier)
- 100K rows written/day (free tier)
- Use for cold data, not hot game state

### KV Best Practices
- Store static config (troop stats, building costs)
- Don't use for player state (use DOs)
- Cache invalidation via TTL

### Scaling
- Each Player DO can handle ~1000 req/sec
- World Region DOs partition by geography
- Combat DOs are ephemeral, scale automatically

## Development Workflow

1. **Modify code** in `src/`
2. **Test locally** with `npm run dev`
3. **Deploy to dev** with `npm run deploy`
4. **Test with s&box client** pointing to `localhost:8787`
5. **Deploy to production** when stable

## Roadmap

**Phase 1: Database & Authentication** ✅ COMPLETE
- [x] JWT authentication system
- [x] Player Durable Objects with state management
- [x] D1 database schema (players, world_tiles, battle_reports, messages)
- [x] WebSocket notifications via Durable Objects
- [x] Complete game data extraction from Dragons of Atlantis wiki

**Phase 2: World Map & NPCs** ✅ COMPLETE
- [x] 1000x1000 world map generation with biomes
- [x] Player city placement on map
- [x] NPC camp system with garrison data
- [x] Tile querying API (viewport, specific tiles, search)
- [x] World region spatial partitioning

**Phase 3: Combat & Marches** ✅ COMPLETE
- [x] Building system with queues and Durable Object alarms
- [x] Garrison requirement for troop training
- [x] Troop training with garrison-based speed formula (DoA wiki accurate)
- [x] Wall stationing mechanic (only stationed troops defend)
- [x] PvP march system with travel time calculations
- [x] Server-authoritative combat resolution
- [x] Resource plundering (10% of unprotected resources)
- [x] Battle reports sent to both attacker and defender
- [x] March return with survivors and loot
- [x] Test speed multiplier for local development (100x faster)

**Phase 4: Research & Progression** ✅ COMPLETE
- [x] Science Center building requirement
- [x] Research queue management (single queue)
- [x] Research prerequisite validation
- [x] Research bonuses applied to combat (Metallurgy, Medicine)
- [x] Resource production bonuses (Agriculture, Woodcraft, etc.)
- [x] Research completion via Durable Object alarms
- [x] Test suite for research system (test-research-system.ps1/.sh)
- [x] 50% refund on research cancellation
- [x] Test speed multiplier applied to research times

**Phase 5: Alliances** ✅ COMPLETE
- [x] Alliance creation and management (create, disband, info, list)
- [x] Alliance member system (invite, join, leave, kick, promote, demote)
- [x] Alliance chat via WebSocket (real-time messaging)
- [x] Alliance diplomacy system (ally, war, NAP relationships)
- [x] Alliance Durable Object with persistent state
- [x] Database schema for alliances, invitations, and diplomacy
- [x] Test suite for alliance system (test-alliance-system.ps1/.sh)
- [ ] Reinforcement mechanics (troops sent to alliance members) - Future phase

**Phase 6: Advanced Combat** ✅ COMPLETE
- [x] Wilderness gathering (collect specific resources by wilderness type: 70% primary, 10% secondary each)
- [x] NPC camp regeneration (10% per 5 min after 15 min delay, 65 min full respawn)
- [x] Scout/spy marches (Clairvoyance required, success vs Sentinel, intelligence reports)
- [x] March slot limits (1 base + 1 per 5 Muster Point levels)
- [x] Guardian Dragon system (dragons join marches for combat bonuses)
  - [x] Dragon combat bonuses (attack/defense multipliers based on type and level)
  - [x] Dragon damage in combat (5-20% health loss based on troop casualties)
  - [x] Dragon healing over time (1% max health per hour when not on marches)
  - [x] Health fighting minimum based on Aerial Combat research (95% to 50%)
- [ ] Battle Arts (dragon special abilities) - Future enhancement

**Phase 7: Economy & Trading** ✅ COMPLETE
- [x] Storage Vault building with raid protection (formula: 5000 * 1.5^(level-1))
- [x] Factory building (unlocks trading at level 1)
- [x] Levitation and Mercantilism research technologies
- [x] Marketplace trading system (create/search/buy/cancel offers)
- [x] Trade duration: 30 minutes (adjusted by speed multiplier)
- [x] Trade slots based on Mercantilism level (max 16)
- [x] Seller fees (gold = quantity sold)
- [x] Tax system with adjustable rates (0-100%, default 50%)
- [x] Happiness calculation (100 - tax_rate + theater_level * 2.5)
- [x] Hourly gold generation (home_capacity * happiness * tax_rate)
- [x] Test suite for all economy features (test-phase7-economy.ps1/.sh)

**Phase 8: Polish & End Game** 📋 TODO
- [ ] Leaderboards (power, kills, resources)
- [ ] Achievement system
- [ ] Timed server events
- [ ] Admin tools and moderation

## Troubleshooting

**"Database not found":**
- Ensure you created the D1 database: `wrangler d1 create atlantis-strategy-db`
- Update `database_id` in `wrangler.toml`

**"KV namespace not found":**
- Create namespace: `wrangler kv:namespace create GAME_CONFIG`
- Update `id` in `wrangler.toml`

**WebSocket connection fails:**
- Check JWT token is valid
- Ensure WebSocket URL is `ws://` (not `wss://`) for localhost
- Check browser console for errors

**"Durable Object not found":**
- Ensure all Durable Objects are exported in `src/workers/index.ts`
- Restart local dev server after adding new DOs

## Resources

- [Cloudflare Workers Docs](https://developers.cloudflare.com/workers/)
- [Durable Objects Guide](https://developers.cloudflare.com/durable-objects/)
- [D1 Database Docs](https://developers.cloudflare.com/d1/)
- [Wrangler CLI Reference](https://developers.cloudflare.com/workers/wrangler/)

## License

MIT
