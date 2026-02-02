# Server-Authoritative Strategy Game Architecture
## Cloudflare Workers + s&box Integration

This document outlines the complete architecture for a Dragons of Atlantis-inspired strategy game with strict server authority using Cloudflare's edge infrastructure.

---

## Core Principles

1. **Zero Trust Client**: Client is a "dumb terminal" - all game logic, RNG, and validation happens server-side
2. **Cloudflare Edge**: Leverage Workers, Durable Objects, D1, KV, and Queues for global low-latency infrastructure
3. **Deterministic Combat**: Same inputs always produce same outputs (server-computed, client can replay for visualization)
4. **Audit Trail**: All player actions logged for anti-cheat and dispute resolution
5. **Real-Time Hybrid**: HTTP for commands, WebSockets for push notifications

---

## Architecture Questions Answered

### 1. State Ownership: Durable Objects Design

**Recommended Pattern: Hybrid Approach**

```
Player Durable Object (1 per player)
├── City State (inner city, outer fields, buildings)
├── Resources (current amounts, production rates)
├── Research Progress
├── Training Queues
├── Dragon State
└── Active Marches (outgoing)

Alliance Durable Object (1 per alliance)
├── Member List
├── Chat History (recent, older moves to D1)
├── Alliance Resources
└── Coordinated Attack Plans

World Region Durable Object (1 per 100x100 tile grid)
├── City Locations
├── NPC Camp States
├── Wilderness Tiles (conquest status)
└── Active Marches (in-region)

Combat Durable Object (1 per combat, ephemeral)
├── Attacker Armies
├── Defender Armies
├── Combat Log Generation
└── Self-destructs after resolution
```

**Why This Works:**
- **Player DO**: Holds all personal state, fast reads/writes for most operations
- **Alliance DO**: Enables real-time chat via WebSocket, coordinates attacks atomically
- **World Region DO**: Spatial partitioning prevents global locks, enables viewport queries
- **Combat DO**: Isolates combat resolution, prevents race conditions on army state

**Cross-Player Atomicity (Attacks, Trades):**
```typescript
// Attack Flow
1. Attacker Player DO validates march (has troops, target valid)
2. Creates march record, deducts troops from city
3. Sends message to World Region DO with arrival time
4. World Region DO schedules alarm for arrival
5. On alarm: World Region DO creates Combat DO
6. Combat DO fetches defender state (read-only), computes outcome
7. Combat DO sends results back to both Player DOs
8. Player DOs update troops/resources transactionally
```

**Key Insight**: Use message passing between DOs rather than direct calls. Queue-based communication ensures eventual consistency without distributed locks.

---

### 2. Offline Simulation: Lazy Calculation

**Recommended: Lazy + Checkpoint Hybrid**

**On Login (Lazy):**
```typescript
class PlayerDurableObject {
  async onLogin(timestamp: number) {
    const lastSeen = this.state.lastLoginTime;
    const elapsed = timestamp - lastSeen;

    // Calculate accumulated resources
    const resources = this.calculateResourceAccumulation(elapsed);

    // Process completed construction/research/training
    const completed = this.processTimedQueues(timestamp);

    // Retrieve attack history from D1 (attacks stored when they happened)
    const attacks = await this.getAttacksSince(lastSeen);

    // Update state
    this.state.resources += resources;
    this.state.buildings = completed.buildings;
    this.state.troops += completed.troops;

    return {
      resources,
      completed,
      attacks // Client renders battle reports
    };
  }
}
```

**Checkpointing for Long Absences:**
- If player offline > 24 hours, use Durable Object Alarms to checkpoint every 6 hours
- Prevents overflow errors on resource caps
- Reduces login calculation time

**Attacks While Offline:**
- Attacker triggers combat → Combat DO resolves immediately
- Result stored in D1 `battle_reports` table with `defender_id` index
- Defender's Player DO NOT woken up (saves compute)
- On defender login: query `battle_reports` WHERE `defender_id = ? AND timestamp > ?`

**Why Lazy Wins:**
- Durable Objects bill per active time → keeping all players always-on is expensive
- Most players login 2-4x/day → wasted ticks for offline players
- Alarms for critical events (march arrivals) only, not resource ticks

---

### 3. March & Combat Pipeline: Durable Object Alarms + Queues

**Recommended: Hybrid Approach**

**Flow:**
```
1. Player clicks "Attack City X"
2. Client sends POST /api/march/start
3. Player DO validates, creates march record
4. Player DO sends march data to World Region DO
5. World Region DO sets Durable Object Alarm for arrival time
6. [30 minutes pass]
7. Alarm fires → World Region DO triggers combat
8. World Region DO creates Combat DO with attacker/defender state
9. Combat DO resolves battle, returns outcome
10. World Region DO sends results to both Player DOs (via Queues)
11. Player DOs update state, send WebSocket notification if online
```

**Why Durable Object Alarms:**
- **Precise Timing**: Fires within 30 seconds of target time (Cloudflare SLA)
- **Persistent**: Survives DO hibernation, guaranteed to fire even if DO evicted
- **No Polling**: More efficient than Cron checking millions of marches

**Fallback with Queues:**
- For non-critical events (construction completion), use Queues with delay
- Queues have longer retry policies, better for bulk processing
- Example: Queue message for "building completed" → Player DO consumes on next activity

**Handling Failures:**
- Alarm missed? World Region DO re-schedules on next activity
- Combat DO crash? Queues retry with exponential backoff
- Player DO unreachable? Store result in D1, deliver on next login

**Code Example:**
```typescript
class WorldRegionDurableObject {
  async scheduleMarchArrival(march: March) {
    // Store march in DO state
    this.state.activeMarches.set(march.id, march);

    // Set alarm for arrival
    const arrivalTime = march.startTime + march.travelDuration;
    await this.state.storage.setAlarm(arrivalTime);
  }

  async alarm() {
    const now = Date.now();

    // Find all marches that should have arrived
    for (const [id, march] of this.state.activeMarches) {
      if (march.arrivalTime <= now) {
        await this.resolveMarchArrival(march);
        this.state.activeMarches.delete(id);
      }
    }

    // Schedule next alarm if more marches pending
    const nextMarch = this.getNextMarchArrival();
    if (nextMarch) {
      await this.state.storage.setAlarm(nextMarch.arrivalTime);
    }
  }

  async resolveMarchArrival(march: March) {
    // Create Combat DO
    const combatId = this.env.COMBAT_DO.idFromName(march.id);
    const combat = this.env.COMBAT_DO.get(combatId);

    // Resolve combat
    const result = await combat.fetch(new Request("https://fake/resolve", {
      method: "POST",
      body: JSON.stringify(march)
    }));

    // Send results via Queue to both players
    await this.env.GAME_QUEUE.send({
      type: "combat_result",
      attackerId: march.attackerId,
      defenderId: march.defenderId,
      result: await result.json()
    });
  }
}
```

---

### 4. Client-Server Protocol: HTTP + WebSocket Hybrid

**Authentication Flow:**
```
1. Player launches s&box game
2. Client requests Steam ticket: `SteamUser.GetAuthSessionTicket()`
3. Client sends ticket to: POST /api/auth/steam
4. Worker validates ticket with Steam API
5. Worker generates JWT with player_id, signs with secret in KV
6. Client stores JWT, includes in all requests as `Authorization: Bearer <jwt>`
```

**HTTP API (Commands):**
```csharp
// s&box Client Code
public class CloudflareAPI {
  private const string BASE_URL = "https://api.yourgame.workers.dev";
  private string jwt;

  public async Task<BuildingResult> StartBuildingUpgrade(string buildingType) {
    var request = Http.RequestAsync($"{BASE_URL}/api/building/upgrade");
    request.Method = "POST";
    request.SetRequestHeader("Authorization", $"Bearer {jwt}");
    request.SetRequestHeader("Content-Type", "application/json");

    var body = new { buildingType, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
    request.Body = JsonSerializer.Serialize(body);

    var response = await request;
    if (!response.IsSuccessStatusCode) {
      throw new Exception($"API Error: {response.StatusDescription}");
    }

    return JsonSerializer.Deserialize<BuildingResult>(response.Body);
  }
}
```

**WebSocket (Push Notifications):**
```typescript
// Worker Code (Durable Object with WebSocket)
class PlayerDurableObject {
  private websocket: WebSocket | null = null;

  async handleWebSocketUpgrade(request: Request) {
    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);

    this.state.acceptWebSocket(server);
    this.websocket = server;

    return new Response(null, { status: 101, webSocket: client });
  }

  async pushNotification(event: GameEvent) {
    if (this.websocket && this.websocket.readyState === 1) {
      this.websocket.send(JSON.stringify(event));
    } else {
      // Store for next login
      await this.state.storage.put(`pending_events`, event);
    }
  }
}
```

```csharp
// s&box Client Code
public class GameNotifications {
  private WebSocket socket;

  public async Task ConnectWebSocket(string jwt) {
    socket = new WebSocket($"wss://api.yourgame.workers.dev/ws");
    socket.SetRequestHeader("Authorization", $"Bearer {jwt}");

    socket.OnMessage += (msg) => {
      var gameEvent = JsonSerializer.Deserialize<GameEvent>(msg);
      HandleEvent(gameEvent);
    };

    await socket.ConnectAsync();
  }

  private void HandleEvent(GameEvent evt) {
    switch (evt.Type) {
      case "attack_incoming":
        ShowAttackWarning(evt.Data);
        break;
      case "building_complete":
        RefreshCityView();
        break;
      case "alliance_chat":
        AppendChatMessage(evt.Data);
        break;
    }
  }
}
```

**Rate Limiting:**
```typescript
// Worker with Rate Limiting
const rateLimiter = new Map<string, number[]>();

async function rateLimit(playerId: string, maxRequests: number, windowMs: number) {
  const now = Date.now();
  const requests = rateLimiter.get(playerId) || [];

  // Remove old requests outside window
  const recent = requests.filter(t => now - t < windowMs);

  if (recent.length >= maxRequests) {
    return new Response("Rate limit exceeded", { status: 429 });
  }

  recent.push(now);
  rateLimiter.set(playerId, recent);
  return null; // Allow request
}

// In Worker handler
const limited = await rateLimit(playerId, 60, 60000); // 60 req/min
if (limited) return limited;
```

**Client Polling Fallback:**
- WebSocket preferred for low latency
- If WebSocket fails: poll GET /api/events?since=<timestamp> every 5 seconds
- Server returns array of events since last poll

---

### 5. World Map at Scale: Spatial Indexing + KV Caching

**Problem:** 10,000 players × 3 outposts = 30,000 cities + 50,000 NPC camps = need to query "what's in viewport (X,Y) to (X+50, Y+50)"

**Solution: Multi-Tier Strategy**

**Tier 1: World Region Durable Objects (Hot Data)**
- Divide world into 100×100 tile regions (e.g., Region_50_50 covers X:5000-5099, Y:5000-5099)
- Each Region DO maintains in-memory map of tiles:
```typescript
class WorldRegionDO {
  private tiles: Map<string, TileData>; // "x_y" -> { type, ownerId, level }

  async getViewport(x1: number, y1: number, x2: number, y2: number) {
    const result = [];
    for (let x = x1; x <= x2; x++) {
      for (let y = y1; y <= y2; y++) {
        const tile = this.tiles.get(`${x}_${y}`);
        if (tile) result.push({ x, y, ...tile });
      }
    }
    return result;
  }
}
```

**Tier 2: D1 Spatial Index (Cold Data, Cross-Region Queries)**
```sql
CREATE TABLE world_tiles (
  x INTEGER NOT NULL,
  y INTEGER NOT NULL,
  tile_type TEXT NOT NULL, -- 'city', 'npc_camp', 'wilderness', 'outpost'
  owner_id TEXT, -- player_id if owned
  level INTEGER DEFAULT 1,
  region_x INTEGER GENERATED ALWAYS AS (x / 100) STORED,
  region_y INTEGER GENERATED ALWAYS AS (y / 100) STORED,
  PRIMARY KEY (x, y)
);

CREATE INDEX idx_region ON world_tiles(region_x, region_y);
CREATE INDEX idx_owner ON world_tiles(owner_id);

-- Query viewport spanning multiple regions
SELECT * FROM world_tiles
WHERE x BETWEEN ? AND ? AND y BETWEEN ? AND ?
LIMIT 2500; -- Max viewport size
```

**Tier 3: KV Pre-Rendered Chunks (Ultra-Fast Reads)**
```typescript
// Pre-render 50×50 chunks as static JSON, update on tile changes
async function updateChunk(chunkX: number, chunkY: number) {
  const tiles = await db.query(
    `SELECT * FROM world_tiles WHERE x BETWEEN ? AND ? AND y BETWEEN ? AND ?`,
    [chunkX * 50, (chunkX + 1) * 50 - 1, chunkY * 50, (chunkY + 1) * 50 - 1]
  );

  await env.KV.put(
    `chunk_${chunkX}_${chunkY}`,
    JSON.stringify(tiles),
    { expirationTtl: 3600 } // Cache 1 hour
  );
}

// Client viewport query
async function getViewport(centerX: number, centerY: number, radius: number) {
  const chunkX = Math.floor(centerX / 50);
  const chunkY = Math.floor(centerY / 50);

  // Fetch surrounding chunks from KV (parallel)
  const chunks = await Promise.all([
    env.KV.get(`chunk_${chunkX}_${chunkY}`),
    env.KV.get(`chunk_${chunkX + 1}_${chunkY}`),
    env.KV.get(`chunk_${chunkX}_${chunkY + 1}`),
    env.KV.get(`chunk_${chunkX + 1}_${chunkY + 1}`)
  ]);

  // Merge and filter to exact viewport
  return mergeChunks(chunks, centerX, centerY, radius);
}
```

**Client-Side Rendering:**
```csharp
// s&box Client
public class WorldMapRenderer {
  private Dictionary<Vector2Int, TileData> visibleTiles = new();

  public async Task UpdateViewport(Vector2 cameraPos, float zoom) {
    var (minX, minY, maxX, maxY) = GetViewportBounds(cameraPos, zoom);

    // Fetch from API
    var tiles = await api.GetViewportTiles(minX, minY, maxX, maxY);

    // Differential update (only render changed tiles)
    foreach (var tile in tiles) {
      if (!visibleTiles.ContainsKey(tile.Pos) || visibleTiles[tile.Pos] != tile) {
        RenderTile(tile);
        visibleTiles[tile.Pos] = tile;
      }
    }

    // Remove tiles outside viewport
    var toRemove = visibleTiles.Keys.Where(k => !IsInViewport(k, minX, minY, maxX, maxY));
    foreach (var key in toRemove) {
      RemoveTile(key);
      visibleTiles.Remove(key);
    }
  }
}
```

**Search/Discovery:**
```sql
-- Find nearest unclaimed wilderness
SELECT x, y,
  ABS(x - ?) + ABS(y - ?) as distance -- Manhattan distance
FROM world_tiles
WHERE tile_type = 'wilderness' AND owner_id IS NULL
ORDER BY distance ASC
LIMIT 10;

-- Find player by name (alliance search)
CREATE INDEX idx_player_name ON players(LOWER(name));
SELECT player_id, name, alliance_id FROM players
WHERE LOWER(name) LIKE LOWER(?) LIMIT 20;
```

---

### 6. Combat Determinism: Server Compute + Client Replay

**Server-Side Combat Resolution:**
```typescript
class CombatDurableObject {
  async resolveCombat(attackers: Army[], defenders: Army[]) {
    // Deterministic seed from march ID + timestamp
    const seed = `${this.marchId}_${this.timestamp}`;
    const rng = new SeededRNG(seed);

    const log: CombatEvent[] = [];
    let turn = 0;

    while (attackers.length > 0 && defenders.length > 0 && turn < 100) {
      // Attacker priority
      for (const unit of attackers) {
        const target = this.selectTarget(unit, defenders, rng);
        const damage = this.calculateDamage(unit, target, rng);
        target.hp -= damage;

        log.push({
          turn,
          attacker: unit.id,
          defender: target.id,
          damage,
          seed: rng.state // Capture RNG state for replay
        });

        if (target.hp <= 0) {
          defenders = defenders.filter(d => d.id !== target.id);
        }
      }

      // Defender counterattack
      for (const unit of defenders) {
        const target = this.selectTarget(unit, attackers, rng);
        const damage = this.calculateDamage(unit, target, rng);
        target.hp -= damage;

        log.push({ turn, attacker: unit.id, defender: target.id, damage, seed: rng.state });

        if (target.hp <= 0) {
          attackers = attackers.filter(a => a.id !== target.id);
        }
      }

      turn++;
    }

    return {
      winner: attackers.length > 0 ? 'attacker' : 'defender',
      survivors: attackers.length > 0 ? attackers : defenders,
      log, // Full deterministic log
      seed // Original seed for client replay
    };
  }

  private calculateDamage(attacker: Unit, defender: Unit, rng: SeededRNG) {
    // Example: Base damage ± 10% variance
    const baseDamage = attacker.attack * (100 / (100 + defender.defense));
    const variance = rng.next() * 0.2 - 0.1; // -10% to +10%
    return Math.floor(baseDamage * (1 + variance));
  }
}

// Seeded RNG for determinism
class SeededRNG {
  private _state: number;

  constructor(seed: string) {
    this._state = this.hashSeed(seed);
  }

  next(): number {
    // Linear Congruential Generator
    this._state = (this._state * 1664525 + 1013904223) % 2**32;
    return this._state / 2**32;
  }

  get state(): number { return this._state; }

  private hashSeed(seed: string): number {
    let hash = 0;
    for (let i = 0; i < seed.length; i++) {
      hash = ((hash << 5) - hash) + seed.charCodeAt(i);
      hash |= 0;
    }
    return Math.abs(hash);
  }
}
```

**Client-Side Replay (Anti-Cheat Safe):**
```csharp
// s&box Client receives combat result from server
public class CombatVisualizer {
  public void ReplayCombat(CombatResult result) {
    // Client CANNOT modify outcome, only visualize
    var scene = CreateCombatScene(result.InitialState);

    foreach (var evt in result.Log) {
      // Animate attack
      AnimateAttack(evt.Attacker, evt.Defender, evt.Damage);

      // Verify determinism (optional dev check)
      if (DevMode) {
        var expectedDamage = RecalculateDamage(evt.Attacker, evt.Defender, evt.Seed);
        if (expectedDamage != evt.Damage) {
          Log.Warning("Combat replay desync!");
        }
      }
    }

    ShowCombatResult(result.Winner, result.Survivors);
  }
}
```

**Why This Is Safe:**
- Client receives outcome + log from server (already resolved)
- Client can replay for pretty animations, but cannot change outcome
- Server-computed outcome is authoritative, stored in D1
- Any client-side tampering only affects local visuals, not game state

**Cheat Vector Analysis:**
- **Can client fake damage?** No, server already resolved combat
- **Can client predict outcomes?** Yes, but irrelevant - they can't change server decision
- **Can client replay faster?** Yes, harmless - just skips animation
- **Recommendation:** Don't send combat log to client if paranoid - just send outcome. Trade-off: less engaging visuals

---

### 7. Audit & Replay: Command Sourcing Pattern

**What to Log:**
```typescript
// Every player action becomes an event
interface GameCommand {
  command_id: string;      // UUID
  player_id: string;
  timestamp: number;
  command_type: string;    // 'build_upgrade', 'train_troops', 'start_march', etc.
  payload: any;            // Command-specific data
  result: 'success' | 'rejected';
  reason?: string;         // If rejected: "insufficient_resources"
  state_hash: string;      // Hash of player state after command
}

// D1 Table
CREATE TABLE command_log (
  command_id TEXT PRIMARY KEY,
  player_id TEXT NOT NULL,
  timestamp INTEGER NOT NULL,
  command_type TEXT NOT NULL,
  payload TEXT, -- JSON
  result TEXT NOT NULL,
  reason TEXT,
  state_hash TEXT,
  FOREIGN KEY (player_id) REFERENCES players(player_id)
);

CREATE INDEX idx_player_commands ON command_log(player_id, timestamp DESC);
```

**Logging in Durable Object:**
```typescript
class PlayerDurableObject {
  async handleCommand(cmd: GameCommand) {
    // Validate command
    const validation = this.validateCommand(cmd);
    if (!validation.valid) {
      await this.logCommand(cmd, 'rejected', validation.reason);
      return { success: false, reason: validation.reason };
    }

    // Execute command
    this.applyCommand(cmd);

    // Log to D1 (async, non-blocking)
    const stateHash = this.hashState();
    await this.logCommand(cmd, 'success', null, stateHash);

    return { success: true };
  }

  private async logCommand(cmd: GameCommand, result: string, reason?: string, stateHash?: string) {
    await this.env.DB.prepare(
      `INSERT INTO command_log VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    ).bind(
      cmd.command_id,
      cmd.player_id,
      cmd.timestamp,
      cmd.command_type,
      JSON.stringify(cmd.payload),
      result,
      reason,
      stateHash
    ).run();
  }

  private hashState(): string {
    const state = {
      resources: this.state.resources,
      buildings: this.state.buildings,
      troops: this.state.troops
    };
    return crypto.subtle.digest('SHA-256', JSON.stringify(state));
  }
}
```

**Replay for Debugging:**
```typescript
// Admin tool: replay player's session from command log
async function replayPlayerSession(playerId: string, fromTime: number, toTime: number) {
  const commands = await db.query(
    `SELECT * FROM command_log WHERE player_id = ? AND timestamp BETWEEN ? AND ? ORDER BY timestamp ASC`,
    [playerId, fromTime, toTime]
  );

  let reconstructedState = await loadPlayerSnapshot(playerId, fromTime);

  for (const cmd of commands) {
    reconstructedState = applyCommand(reconstructedState, cmd);

    // Verify hash matches
    if (cmd.state_hash !== hashState(reconstructedState)) {
      console.error(`State desync at ${cmd.timestamp}!`);
    }
  }

  return reconstructedState;
}
```

**State Snapshots (Periodic):**
```typescript
// Every hour, save full state snapshot
async function snapshotPlayerState(playerId: string) {
  const state = await getPlayerState(playerId);

  await db.prepare(
    `INSERT INTO state_snapshots (player_id, timestamp, state_json, state_hash) VALUES (?, ?, ?, ?)`
  ).bind(
    playerId,
    Date.now(),
    JSON.stringify(state),
    hashState(state)
  ).run();
}

// Replay from nearest snapshot instead of genesis
async function replayFromSnapshot(playerId: string, targetTime: number) {
  const snapshot = await db.query(
    `SELECT * FROM state_snapshots WHERE player_id = ? AND timestamp <= ? ORDER BY timestamp DESC LIMIT 1`,
    [playerId, targetTime]
  );

  const commands = await getCommandsSince(playerId, snapshot.timestamp);
  return replayCommands(snapshot.state_json, commands);
}
```

**What NOT to Log:**
- Raw passwords/API keys (obvs)
- WebSocket ping/pong (noise)
- GET requests for static data (viewport queries, troop stats)
- Only log state-mutating commands

---

## Cloudflare Stack Best Practices

### Workers
- **Stateless**: Each request is isolated, use env bindings to access DO/KV/D1
- **Cold Starts**: First request to new edge location ~10ms, subsequent <1ms
- **CPU Limit**: 50ms per request on free tier, 30s on paid (use for combat resolution)
- **Cron Triggers**: For daily resets, leaderboard updates, NPC respawns

### Durable Objects
- **Billing**: Charged per active duration (websocket = always active, sleep when inactive)
- **Alarms**: Use for timed events (march arrivals), 30s precision
- **Storage**: Transactional key-value, supports lists/maps, 128 keys per txn
- **Hibernation**: DO hibernates after 10s inactivity, alarm wakes it up

### D1
- **Read Performance**: ~5ms from nearest edge, replicated globally
- **Write Latency**: ~20-50ms (replicates to primary region)
- **Batch Writes**: Use transactions for atomicity, `.batch()` for bulk
- **Limits**: 100k rows/query, 1MB response size

### KV
- **Read Speed**: <1ms from edge cache
- **Write Propagation**: Eventually consistent, 60s to global
- **Use Cases**: Static game config (troop stats, building costs), not player state
- **Expiration**: Set TTL for cache invalidation

### Queues
- **Delivery**: At-least-once, use idempotency keys
- **Batch Processing**: Consume up to 100 messages per invocation
- **Use Cases**: Combat results, construction completion, batch notifications

---

## Anti-Cheat Checklist

- ✅ All game logic server-side (client sends intents, server validates)
- ✅ Resource calculations server-computed (client displays, doesn't calculate)
- ✅ Combat outcomes deterministic but server-resolved
- ✅ RNG seeded server-side, never client-provided seeds
- ✅ Timestamps server-generated (reject client timestamps)
- ✅ Rate limiting on all endpoints (per-player, per-IP)
- ✅ Command log for audit trail (replay player sessions)
- ✅ State hashing to detect tampering (verify snapshots)
- ✅ WebSocket authentication (JWT verified on every message)
- ✅ Input validation (building IDs exist, troop counts positive, coordinates in-bounds)
- ✅ No client-side prediction for strategic decisions (only visual smoothing)

---

## Implementation Roadmap

**Phase 1: Core Infrastructure (Week 1-2)**
1. Set up Cloudflare Workers project
2. Implement authentication (Steam ticket → JWT)
3. Create Player Durable Object with basic state
4. Build s&box API client wrapper
5. Add WebSocket connection for notifications

**Phase 2: City & Resources (Week 3-4)**
1. Implement building system (upgrade queues, worker limits)
2. Resource production calculation (lazy on login)
3. D1 schema for buildings, research, troops
4. KV config for building costs, production rates
5. s&box UI for city management

**Phase 3: Combat & Marches (Week 5-6)**
1. World Region Durable Objects with spatial index
2. March system with DO alarms
3. Combat resolution in Combat DO
4. Battle replay system on client
5. NPC camp regeneration

**Phase 4: Advanced Features (Week 7-8)**
1. Alliance system with chat DO
2. Dragon system (eggs, armor, outposts)
3. Tech tree with percentage bonuses
4. World map renderer with viewport optimization
5. Leaderboards in D1

**Phase 5: Polish & Launch (Week 9-10)**
1. Rate limiting and anti-cheat hardening
2. Command logging and audit tools
3. Load testing with Cloudflare's test suite
4. Tutorial flow
5. Soft launch with monitoring

---

## Cost Estimation (Cloudflare)

**Free Tier:**
- Workers: 100k requests/day
- Durable Objects: 1M writes, 100M reads/month
- D1: 100k rows read, 100k rows written/day
- KV: 100k reads, 1k writes/day

**Paid (1000 concurrent players):**
- Workers: $5/month (10M requests)
- Durable Objects: ~$50/month (30M writes for player state)
- D1: ~$10/month (5M reads, 500k writes)
- KV: ~$5/month (10M reads)
- **Total: ~$70/month for 1k players** (scales linearly)

**Comparison to Traditional Stack:**
- AWS EC2 + RDS: ~$200/month minimum
- Heroku + Postgres: ~$75/month (but slower, no edge distribution)
- **Cloudflare wins on cost + latency**

---

## References
- [Cloudflare Durable Objects](https://developers.cloudflare.com/durable-objects/)
- [Cloudflare D1 Database](https://developers.cloudflare.com/d1/)
- [s&box Networking Guide](https://wiki.facepunch.com/sbox/Networking)
- [Server-Authoritative Game Architecture (GDC Talk)](https://www.youtube.com/watch?v=W3aieHjyNvw)
