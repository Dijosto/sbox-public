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

**Phase 1: Core Infrastructure** ✅
- [x] Authentication with Steam
- [x] Player Durable Objects
- [x] Basic state management
- [x] WebSocket notifications
- [x] Complete game data extraction from Dragons of Atlantis wiki

**Phase 2: Game Systems** ✅ (Complete)
- [x] Game configuration utilities (buildings, troops, research, resources)
- [x] Building definitions with cost formulas (12+ building types, 35 levels)
- [x] Troop stats & combat calculations (11+ troop types with full stats)
- [x] Research tree with bonuses (13+ technologies, 20 levels each)
- [x] Resource production & economy formulas (taxation, population, happiness)
- [x] Building upgrade logic with queue management and worker limits
- [x] Troop training queue system with garrison-based speed timers
- [x] Research queue system with prerequisites and dependency validation
- [x] Durable Object alarms for automatic timer completion

**Phase 3: Combat & Marches** 📋
- [ ] March scheduling with DO alarms
- [ ] Combat resolution in Combat DO
- [ ] Battle replay system
- [ ] NPC camp regeneration

**Phase 4: World Map** 📋
- [ ] Spatial indexing in D1
- [ ] Viewport caching in KV
- [ ] Wilderness conquest
- [ ] Alliance territories

**Phase 5: Polish** 📋
- [ ] Rate limiting
- [ ] Leaderboards
- [ ] Battle reports UI
- [ ] Admin tools

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
