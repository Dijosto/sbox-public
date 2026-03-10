# World Map & PvP Testing Guide

This guide covers testing the Phase 3 world map integration and PvP combat system.

## Quick Start

### 1. Generate and Load World Map

```bash
# Make scripts executable
chmod +x setup-world.sh test-world-pvp.sh

# Generate world map and load into database
./setup-world.sh
```

This will:
- Generate **715 Anthropus camps** (levels 1-10)
- Generate **10,000 wilderness tiles** (forest, savanna, hills, mountains, plains)
- Load all data into D1 database
- Verify the data

### 2. Start Development Server

```bash
npm run dev
```

Server runs on `http://localhost:8787`

### 3. Run Comprehensive Tests

**Linux/Mac/Git Bash:**
```bash
./test-world-pvp.sh
```

**Windows PowerShell:**
```powershell
.\test-world-pvp.ps1
```

## What the Test Script Does

The comprehensive test script validates the entire Phase 3 system:

### Phase 1: World Setup
1. **Health Check** - Verify server is running
2. **Player Registration** - Create 2 test players
   - Player 1: "Attacker"
   - Player 2: "Defender"
3. **Spawn Verification** - Confirm both players spawned on random empty tiles

### Phase 2: Map Queries
4. **Get Player Coordinates** - Retrieve city locations from player state
5. **Query Region Tiles** - Get all tiles in Player 1's region (100x100 grid)
   - Shows cities, NPC camps, wilderness tiles
6. **Query Specific Tile** - Get details for Player 2's city tile
7. **Search Players** - Find players by name

### Phase 3: Combat Preparation
8. **Train Defender Troops** - Give Player 2 garrison (100 conscripts)
9. **Train Attacker Troops** - Give Player 1 army (150 conscripts)

### Phase 4: PvP Combat
10. **Send March** - Player 1 attacks Player 2's city
    - Calculate distance and travel time
    - Deduct troops from attacker
11. **Wait for Arrival** - March travels across map
12. **Combat Resolution**:
    - Query defender's city and troops
    - Resolve deterministic combat
    - Calculate plunder (10% of resources)
    - Deduct losses from defender
    - Send battle reports to both players
13. **Return March** - Survivors return home with loot

### Phase 5: Verification
14. **Check Battle Reports** - Verify both players received messages
15. **Check Final States**:
    - Attacker: Should have fewer troops, gained resources
    - Defender: Should have fewer troops, lost resources

## Manual Testing

### Query World Map

Get tiles in a region:
```bash
curl "http://localhost:8787/api/world/tiles?regionX=3&regionY=4" | jq
```

Get specific tile:
```bash
curl "http://localhost:8787/api/world/tile/500/500" | jq
```

Search for players:
```bash
curl "http://localhost:8787/api/world/search?playerName=Player" | jq
```

### PvP Combat Flow

1. **Create two players** (see test script)
2. **Get their coordinates** from player state
3. **Train troops** for both players
4. **Send attack march**:
```bash
curl -X POST "http://localhost:8787/api/player/march/send" \
  -H "Authorization: Bearer $ATTACKER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "destination": { "x": 520, "y": 510 },
    "troops": [
      { "troopType": "conscript", "quantity": 100 }
    ],
    "marchType": "attack",
    "targetType": "player"
  }'
```

5. **Wait for march** to complete (check travelTime from response)
6. **Check messages**:
```bash
curl "http://localhost:8787/api/player/messages" \
  -H "Authorization: Bearer $TOKEN" | jq
```

### NPC Combat (Still Works)

Attack an Anthropus camp:
```bash
curl -X POST "http://localhost:8787/api/player/march/send" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "destination": { "x": 500, "y": 500 },
    "troops": [
      { "troopType": "conscript", "quantity": 50 }
    ],
    "marchType": "attack",
    "targetType": "npc"
  }'
```

## World Map Statistics

After running `setup-world.sh`, you should have:

### NPC Camps (715 total)
- Level 1: 200 camps (very common)
- Level 2: 150 camps
- Level 3: 120 camps
- Level 4: 80 camps
- Level 5: 60 camps
- Level 6: 40 camps
- Level 7: 30 camps
- Level 8: 20 camps
- Level 9: 10 camps
- Level 10: 5 camps (extremely rare)

### Wilderness Tiles (10,000 total)
Distributed across 5 resource types:
- **Forest** (lumber production +5% to +50%)
- **Savanna** (food production +5% to +50%)
- **Hills** (stone production +5% to +50%)
- **Mountains** (metal production +5% to +50%)
- **Plains** (outpost placement, no bonus)

Note: Default is 10,000 tiles (can be increased by modifying `worldgen.ts`)

## Expected Test Results

### Successful Test Output

```
✅ Server health check passes
✅ Both players created with unique city coordinates
✅ Map queries return tiles with correct data
✅ Player search finds both test players
✅ Troops trained successfully
✅ PvP march sent with travel time calculated
✅ Combat resolves after march arrival
✅ Battle reports sent to both players
✅ Attacker gains loot, defender loses resources
✅ Troop losses applied correctly
```

### Combat Example

**Before Battle:**
- Attacker: 150 conscripts
- Defender: 100 conscripts, 10,000 food

**After Battle (Attacker Victory):**
- Attacker: ~120 conscripts (30 losses), +1,000 food (10% plunder)
- Defender: 0 conscripts (100 losses), 9,000 food

**Battle Reports:**
- Attacker receives: "Your forces defeated Defender"
- Defender receives: "Attacker attacked your city. Your city was plundered!"

## Troubleshooting

### "No such table: world_tiles"
Run `./setup-world.sh` to generate and load the world map.

### "March failed - no target found"
Ensure:
- Target coordinates point to a city tile
- World map is loaded
- Target player exists

### "March never arrives"
Check:
- Wrangler dev server is running
- Durable Object alarms are enabled
- Wait for `travelTime` seconds (shown in march response)

### Database is too slow
The world generation creates a large dataset. For faster testing:
- Current default: 10,000 wilderness tiles
- For even faster testing: Reduce count in `worldgen.ts` (change default 10000 to 5000)
- Regenerate world: `./setup-world.sh`

## Database Queries

Check world data directly:

```bash
# Count tiles by type
wrangler d1 execute atlantis-strategy-db --local \
  --command="SELECT tile_type, COUNT(*) FROM world_tiles GROUP BY tile_type"

# Find all player cities
wrangler d1 execute atlantis-strategy-db --local \
  --command="SELECT x, y, owner_id FROM world_tiles WHERE tile_type='city'"

# Find level 10 Anthropus camps
wrangler d1 execute atlantis-strategy-db --local \
  --command="SELECT x, y, level FROM npc_camps WHERE level=10"

# Check recent battle reports
wrangler d1 execute atlantis-strategy-db --local \
  --command="SELECT * FROM battle_reports ORDER BY timestamp DESC LIMIT 5"
```

## Performance Notes

- **Map Generation**: Takes ~5-10 seconds
- **Database Load**: Takes ~20-30 seconds for 10k+ records
- **Region Query**: Returns <1000 tiles in <100ms
- **PvP Combat**: Resolves in <500ms including both player DO queries

## Game Mechanics Notes

- **Starting Resources**: Players start with 10,000 food/wood/stone/metal and 1,000 gold
- **Resource Caps**: No hard storage caps (matches Dragons of Atlantis)
- **Storage Vault**: Protects resources from raids (not storage capacity)

## Next Steps

After testing world map and PvP:
1. Test NPC combat with new Anthropus troops
2. Test wilderness gathering
3. Test player search and targeting
4. Implement client-side map rendering
5. Add alliance system (Phase 4)
