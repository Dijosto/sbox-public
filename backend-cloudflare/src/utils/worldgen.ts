/**
 * World Map Generation for Dragons of Atlantis Clone
 * Generates a 750x750 world with wilderness, NPC camps, and spawn locations
 */

export interface WorldTile {
  x: number;
  y: number;
  tile_type: 'empty' | 'wilderness' | 'npc_camp' | 'city' | 'outpost';
  owner_id?: string;
  level: number;
  resource_type?: 'forest' | 'savanna' | 'hills' | 'mountains' | 'plains';
  resource_bonus?: number;
}

export interface AnthropusCamp {
  camp_id: string;
  x: number;
  y: number;
  camp_type: string;
  level: number;
  garrison: string; // JSON
  resources: string; // JSON
  max_attacks_per_day: number;
}

// Map dimensions (wraps around like Dragons of Atlantis)
export const MAP_SIZE = 750;
export const MAP_REGIONS = Math.ceil(MAP_SIZE / 100); // 8x8 regions for indexing

/**
 * Seeded random number generator for reproducible world generation
 */
class SeededRandom {
  private seed: number;

  constructor(seed: number) {
    this.seed = seed;
  }

  next(): number {
    this.seed = (this.seed * 9301 + 49297) % 233280;
    return this.seed / 233280;
  }

  nextInt(min: number, max: number): number {
    return Math.floor(this.next() * (max - min + 1)) + min;
  }

  nextFloat(min: number, max: number): number {
    return this.next() * (max - min) + min;
  }
}

/**
 * Anthropus camp data by level (from Dragons of Atlantis wiki)
 */
const ANTHROPUS_CAMP_DATA: Record<number, {
  garrison: Record<string, { quantity: number }>;
  resources: { food: number; wood: number; stone: number; metal: number; gold: number };
  max_attacks: number;
}> = {
  1: {
    garrison: {
      brat: { quantity: 1500 },
      cannibal: { quantity: 500 },
      stench: { quantity: 500 },
      she_devil: { quantity: 1000 },
      clubber: { quantity: 1000 }
    },
    resources: { food: 112500, wood: 5000, stone: 500, metal: 500, gold: 2500 },
    max_attacks: 10
  },
  2: {
    garrison: {
      brat: { quantity: 3000 },
      cannibal: { quantity: 1000 },
      stench: { quantity: 1000 },
      she_devil: { quantity: 2000 },
      clubber: { quantity: 2000 },
      hurler: { quantity: 1500 }
    },
    resources: { food: 168750, wood: 7500, stone: 750, metal: 750, gold: 3750 },
    max_attacks: 9
  },
  3: {
    garrison: {
      brat: { quantity: 7500 },
      cannibal: { quantity: 2500 },
      stench: { quantity: 2500 },
      she_devil: { quantity: 5000 },
      clubber: { quantity: 5000 },
      hurler: { quantity: 7500 }
    },
    resources: { food: 225000, wood: 10000, stone: 1000, metal: 1000, gold: 5000 },
    max_attacks: 8
  },
  4: {
    garrison: {
      brat: { quantity: 15000 },
      cannibal: { quantity: 5000 },
      stench: { quantity: 5000 },
      she_devil: { quantity: 10000 },
      clubber: { quantity: 10000 },
      hurler: { quantity: 15000 },
      shredder: { quantity: 10000 }
    },
    resources: { food: 393750, wood: 17500, stone: 1750, metal: 1750, gold: 8750 },
    max_attacks: 7
  },
  5: {
    garrison: {
      brat: { quantity: 30000 },
      cannibal: { quantity: 10000 },
      stench: { quantity: 10000 },
      she_devil: { quantity: 20000 },
      clubber: { quantity: 20000 },
      hurler: { quantity: 30000 },
      shredder: { quantity: 40000 }
    },
    resources: { food: 562500, wood: 25000, stone: 2500, metal: 2500, gold: 12500 },
    max_attacks: 5
  },
  6: {
    garrison: {
      brat: { quantity: 60000 },
      cannibal: { quantity: 20000 },
      stench: { quantity: 15000 },
      she_devil: { quantity: 30000 },
      clubber: { quantity: 30000 },
      hurler: { quantity: 45000 },
      shredder: { quantity: 60000 },
      launcher: { quantity: 30000 }
    },
    resources: { food: 618750, wood: 27500, stone: 2750, metal: 2750, gold: 13750 },
    max_attacks: 4
  },
  7: {
    garrison: {
      brat: { quantity: 150000 },
      cannibal: { quantity: 50000 },
      stench: { quantity: 30000 },
      she_devil: { quantity: 60000 },
      clubber: { quantity: 60000 },
      hurler: { quantity: 90000 },
      shredder: { quantity: 120000 },
      launcher: { quantity: 90000 }
    },
    resources: { food: 787500, wood: 35000, stone: 3500, metal: 3500, gold: 17500 },
    max_attacks: 3
  },
  8: {
    garrison: {
      brat: { quantity: 300000 },
      cannibal: { quantity: 100000 },
      stench: { quantity: 60000 },
      she_devil: { quantity: 120000 },
      clubber: { quantity: 120000 },
      hurler: { quantity: 180000 },
      shredder: { quantity: 240000 },
      launcher: { quantity: 125000 },
      gnasher: { quantity: 75000 }
    },
    resources: { food: 900000, wood: 40000, stone: 4000, metal: 4000, gold: 20000 },
    max_attacks: 2
  },
  9: {
    garrison: {
      brat: { quantity: 525000 },
      cannibal: { quantity: 175000 },
      stench: { quantity: 90000 },
      she_devil: { quantity: 180000 },
      clubber: { quantity: 180000 },
      hurler: { quantity: 270000 },
      shredder: { quantity: 360000 },
      launcher: { quantity: 187500 },
      gnasher: { quantity: 187500 }
    },
    resources: { food: 1012500, wood: 45000, stone: 4500, metal: 4500, gold: 22500 },
    max_attacks: 2
  },
  10: {
    garrison: {
      brat: { quantity: 750000 },
      cannibal: { quantity: 250000 },
      stench: { quantity: 120000 },
      she_devil: { quantity: 240000 },
      clubber: { quantity: 240000 },
      hurler: { quantity: 360000 },
      shredder: { quantity: 480000 },
      launcher: { quantity: 250000 },
      gnasher: { quantity: 300000 }
    },
    resources: { food: 1125000, wood: 50000, stone: 5000, metal: 5000, gold: 25000 },
    max_attacks: 1
  }
};

/**
 * Camp distribution by level (approximate counts for 750x750 map)
 * Scaled to ~20,000 total camps to match wilderness density
 */
const CAMP_DISTRIBUTION = {
  1: 5000,   // Very common - starter camps
  2: 4000,   // Common
  3: 3500,   // Common
  4: 2500,   // Uncommon
  5: 2000,   // Uncommon
  6: 1500,   // Rare
  7: 1000,   // Rare
  8: 400,    // Very rare
  9: 80,     // Very rare
  10: 20     // Extremely rare - legendary camps
};

/**
 * Generate Anthropus camps with level distribution
 */
export function generateAnthropusCamps(seed: number = 12345): AnthropusCamp[] {
  const rng = new SeededRandom(seed);
  const camps: AnthropusCamp[] = [];
  const usedCoordinates = new Set<string>();

  // Generate camps for each level
  for (const [levelStr, count] of Object.entries(CAMP_DISTRIBUTION)) {
    const level = parseInt(levelStr);
    const campData = ANTHROPUS_CAMP_DATA[level];

    for (let i = 0; i < count; i++) {
      let x: number, y: number, coordKey: string;
      let attempts = 0;

      // Find unused coordinates (avoid overlap)
      do {
        x = rng.nextInt(50, MAP_SIZE - 50); // Keep away from edges
        y = rng.nextInt(50, MAP_SIZE - 50);
        coordKey = `${x},${y}`;
        attempts++;
      } while (usedCoordinates.has(coordKey) && attempts < 100);

      if (attempts >= 100) continue; // Skip if can't find spot

      usedCoordinates.add(coordKey);

      camps.push({
        camp_id: `anthropus_${level}_${x}_${y}`,
        x,
        y,
        camp_type: 'anthropus',
        level,
        garrison: JSON.stringify(campData.garrison),
        resources: JSON.stringify(campData.resources),
        max_attacks_per_day: campData.max_attacks
      });
    }
  }

  return camps;
}

/**
 * Generate wilderness tiles with resource types and levels
 * Default 10,000 for minimal testing (use 100,000+ for production map)
 */
export function generateWildernessTiles(
  seed: number,
  excludedCoordinates: Set<string>,
  count: number = 10000
): WorldTile[] {
  const rng = new SeededRandom(seed + 1000);
  const tiles: WorldTile[] = [];
  const resourceTypes: ('forest' | 'savanna' | 'hills' | 'mountains' | 'plains')[] = [
    'forest',
    'savanna',
    'hills',
    'mountains',
    'plains'
  ];

  for (let i = 0; i < count; i++) {
    let x: number, y: number, coordKey: string;
    let attempts = 0;

    do {
      x = rng.nextInt(0, MAP_SIZE - 1);
      y = rng.nextInt(0, MAP_SIZE - 1);
      coordKey = `${x},${y}`;
      attempts++;
    } while (excludedCoordinates.has(coordKey) && attempts < 100);

    if (attempts >= 100) continue;

    excludedCoordinates.add(coordKey);

    const resourceType = resourceTypes[rng.nextInt(0, resourceTypes.length - 1)];
    const level = rng.nextInt(1, 10);
    const resourceBonus = resourceType === 'plains' ? 0 : 5 * level; // 5% to 50%

    tiles.push({
      x,
      y,
      tile_type: 'wilderness',
      level,
      resource_type: resourceType,
      resource_bonus: resourceBonus
    });
  }

  return tiles;
}

/**
 * Generate SQL INSERT statements for world map (batched for SQLite limits)
 */
export function generateWorldMapSQL(seed: number = 12345, wildernessCount: number = 10000): {
  campInserts: string;
  tileInserts: string;
  campCount: number;
  wildernessCount: number;
} {
  const camps = generateAnthropusCamps(seed);
  const usedCoordinates = new Set<string>();

  // Mark camp coordinates as used
  camps.forEach(camp => usedCoordinates.add(`${camp.x},${camp.y}`));

  // Generate wilderness tiles
  const wildernessTiles = generateWildernessTiles(seed, usedCoordinates, wildernessCount);

  // Generate SQL for camps (batch in groups of 100 to avoid SQLITE_TOOBIG)
  const campInsertStatements: string[] = [];
  const BATCH_SIZE = 100;

  for (let i = 0; i < camps.length; i += BATCH_SIZE) {
    const batch = camps.slice(i, i + BATCH_SIZE);
    const campValues = batch.map(camp => {
      const escapedGarrison = camp.garrison.replace(/'/g, "''");
      const escapedResources = camp.resources.replace(/'/g, "''");
      return `  ('${camp.camp_id}', ${camp.x}, ${camp.y}, '${camp.camp_type}', ${camp.level}, '${escapedGarrison}', '${escapedResources}', ${camp.max_attacks_per_day})`;
    });
    campInsertStatements.push(
      `INSERT OR IGNORE INTO npc_camps (camp_id, x, y, camp_type, level, garrison, resources, max_attacks_per_day) VALUES\n${campValues.join(',\n')};`
    );
  }

  const campInserts = campInsertStatements.join('\n\n');

  // Generate SQL for tiles (batch in groups of 500)
  const tileInsertStatements: string[] = [];
  const allTiles: string[] = [];

  // Add camp tiles
  camps.forEach(camp => {
    allTiles.push(`  (${camp.x}, ${camp.y}, 'npc_camp', ${camp.level}, NULL, 0)`);
  });

  // Add wilderness tiles
  wildernessTiles.forEach(tile => {
    allTiles.push(`  (${tile.x}, ${tile.y}, 'wilderness', ${tile.level}, '${tile.resource_type}', ${tile.resource_bonus})`);
  });

  // Batch tiles into multiple INSERT statements
  const TILE_BATCH_SIZE = 500;
  for (let i = 0; i < allTiles.length; i += TILE_BATCH_SIZE) {
    const batch = allTiles.slice(i, i + TILE_BATCH_SIZE);
    tileInsertStatements.push(
      `INSERT OR IGNORE INTO world_tiles (x, y, tile_type, level, resource_type, resource_bonus) VALUES\n${batch.join(',\n')};`
    );
  }

  const tileInserts = tileInsertStatements.join('\n\n');

  return {
    campInserts,
    tileInserts,
    campCount: camps.length,
    wildernessCount: wildernessTiles.length
  };
}

/**
 * Find a random empty tile for player spawn
 */
export function findRandomSpawnLocation(seed: number, excludedCoordinates: Set<string>): { x: number; y: number } {
  const rng = new SeededRandom(seed);
  let x: number, y: number, coordKey: string;
  let attempts = 0;
  const maxAttempts = 1000;

  do {
    x = rng.nextInt(100, MAP_SIZE - 100); // Keep away from edges
    y = rng.nextInt(100, MAP_SIZE - 100);
    coordKey = `${x},${y}`;
    attempts++;
  } while (excludedCoordinates.has(coordKey) && attempts < maxAttempts);

  if (attempts >= maxAttempts) {
    // Fallback to safe default zone if can't find empty spot
    return { x: 375, y: 375 }; // Center of map
  }

  return { x, y };
}

/**
 * Check if coordinates are within map bounds (with wrapping)
 */
export function normalizeCoordinates(x: number, y: number): { x: number; y: number } {
  return {
    x: ((x % MAP_SIZE) + MAP_SIZE) % MAP_SIZE,
    y: ((y % MAP_SIZE) + MAP_SIZE) % MAP_SIZE
  };
}

/**
 * Calculate distance between two points (Euclidean, accounts for wrapping)
 */
export function calculateWrappedDistance(x1: number, y1: number, x2: number, y2: number): number {
  // Calculate direct distance
  const dx = Math.abs(x2 - x1);
  const dy = Math.abs(y2 - y1);

  // Calculate wrapped distance
  const wrappedDx = Math.min(dx, MAP_SIZE - dx);
  const wrappedDy = Math.min(dy, MAP_SIZE - dy);

  return Math.sqrt(wrappedDx * wrappedDx + wrappedDy * wrappedDy);
}
