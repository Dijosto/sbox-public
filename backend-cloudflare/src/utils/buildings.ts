/**
 * Game Configuration - Building Definitions
 * Based on Dragons of Atlantis game data
 */

export interface BuildingConfig {
  id: string;
  name: string;
  category: 'military' | 'resource' | 'support' | 'defense';
  zone: 'inner' | 'outer' | 'both';
  maxLevel: number;
  levels: BuildingLevel[];
}

export interface BuildingLevel {
  level: number;
  cost: ResourceCost;
  buildTime: number; // seconds
  prerequisites: Prerequisite[];
  production?: number; // For resource buildings (per hour)
  capacity?: number; // For homes, storage
  bonus?: string; // Description of bonus
}

export interface ResourceCost {
  food?: number;
  wood?: number;
  stone?: number;
  metal?: number;
  gold?: number;
}

export interface Prerequisite {
  type: 'building' | 'research';
  id: string;
  level: number;
}

/**
 * Building Definitions
 */
export const BUILDINGS: Record<string, BuildingConfig> = {
  // Core Buildings
  fortress: {
    id: 'fortress',
    name: 'Fortress',
    category: 'defense',
    zone: 'inner',
    maxLevel: 35,
    levels: generateFortressLevels()
  },

  // Resource Production
  farm: {
    id: 'farm',
    name: 'Farm',
    category: 'resource',
    zone: 'outer',
    maxLevel: 35,
    levels: generateResourceBuildingLevels('food')
  },

  lumbermill: {
    id: 'lumbermill',
    name: 'Lumbermill',
    category: 'resource',
    zone: 'outer',
    maxLevel: 35,
    levels: generateResourceBuildingLevels('wood')
  },

  quarry: {
    id: 'quarry',
    name: 'Quarry',
    category: 'resource',
    zone: 'outer',
    maxLevel: 35,
    levels: generateResourceBuildingLevels('stone')
  },

  mine: {
    id: 'mine',
    name: 'Mine',
    category: 'resource',
    zone: 'outer',
    maxLevel: 35,
    levels: generateResourceBuildingLevels('metal')
  },

  // Military
  garrison: {
    id: 'garrison',
    name: 'Garrison',
    category: 'military',
    zone: 'inner',
    maxLevel: 35,
    levels: generateGarrisonLevels()
  },

  musterPoint: {
    id: 'musterPoint',
    name: 'Muster Point',
    category: 'military',
    zone: 'inner',
    maxLevel: 30,
    levels: generateMusterPointLevels()
  },

  // Support Buildings
  home: {
    id: 'home',
    name: 'Home',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateHomeLevels()
  },

  scienceCenter: {
    id: 'scienceCenter',
    name: 'Science Center',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateScienceCenterLevels()
  },

  storageVault: {
    id: 'storageVault',
    name: 'Storage Vault',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateStorageVaultLevels()
  },

  theater: {
    id: 'theater',
    name: 'Theater',
    category: 'support',
    zone: 'inner',
    maxLevel: 10,
    levels: generateTheaterLevels()
  },

  metalsmith: {
    id: 'metalsmith',
    name: 'Metalsmith',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateMetalsmithLevels()
  },

  factory: {
    id: 'factory',
    name: 'Factory',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateFactoryLevels()
  },

  rookery: {
    id: 'rookery',
    name: 'Rookery',
    category: 'support',
    zone: 'inner',
    maxLevel: 35,
    levels: generateRookeryLevels()
  }
};

/**
 * Helper functions to generate building level data
 */

function generateFortressLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(100 * Math.pow(1.5, level - 1)),
        stone: Math.floor(100 * Math.pow(1.5, level - 1)),
        metal: Math.floor(50 * Math.pow(1.5, level - 1)),
        gold: Math.floor(50 * Math.pow(1.4, level - 1))
      },
      buildTime: Math.floor(60 * Math.pow(1.3, level - 1)), // Scales exponentially
      prerequisites: [],
      bonus: `Unlocks higher level buildings and wilderness conquests`
    });
  }

  return levels;
}

function generateResourceBuildingLevels(resourceType: string): BuildingLevel[] {
  const levels: BuildingLevel[] = [];
  const baseProduction = 100; // Base production per hour

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(80 * Math.pow(1.4, level - 1)),
        stone: Math.floor(60 * Math.pow(1.4, level - 1)),
        metal: Math.floor(40 * Math.pow(1.4, level - 1))
      },
      buildTime: Math.floor(45 * Math.pow(1.25, level - 1)),
      prerequisites: level > 1 ? [] : [],
      production: Math.floor(baseProduction * Math.pow(1.3, level - 1))
    });
  }

  return levels;
}

function generateGarrisonLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(150 * Math.pow(1.5, level - 1)),
        stone: Math.floor(100 * Math.pow(1.5, level - 1)),
        metal: Math.floor(75 * Math.pow(1.5, level - 1))
      },
      buildTime: Math.floor(90 * Math.pow(1.3, level - 1)),
      prerequisites: level > 5 ? [{ type: 'building', id: 'fortress', level: Math.floor(level / 2) }] : [],
      bonus: `Training speed increased`
    });
  }

  return levels;
}

function generateMusterPointLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 30; level++) {
    const capacity = Math.floor(10000 * Math.pow(1.25, level - 1));

    levels.push({
      level,
      cost: {
        wood: Math.floor(200 * Math.pow(1.6, level - 1)),
        stone: Math.floor(150 * Math.pow(1.6, level - 1)),
        metal: Math.floor(100 * Math.pow(1.6, level - 1))
      },
      buildTime: Math.floor(120 * Math.pow(1.35, level - 1)),
      prerequisites: [],
      capacity,
      bonus: `March capacity: ${capacity.toLocaleString()}`
    });
  }

  return levels;
}

function generateHomeLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    const capacity = Math.floor(100 * Math.pow(1.4, level - 1));

    levels.push({
      level,
      cost: {
        wood: Math.floor(50 * Math.pow(1.35, level - 1)),
        stone: Math.floor(40 * Math.pow(1.35, level - 1))
      },
      buildTime: Math.floor(30 * Math.pow(1.2, level - 1)),
      prerequisites: [],
      capacity,
      bonus: `Population capacity: ${capacity.toLocaleString()}`
    });
  }

  return levels;
}

function generateScienceCenterLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(200 * Math.pow(1.6, level - 1)),
        stone: Math.floor(150 * Math.pow(1.6, level - 1)),
        metal: Math.floor(100 * Math.pow(1.6, level - 1)),
        gold: Math.floor(100 * Math.pow(1.5, level - 1))
      },
      buildTime: Math.floor(180 * Math.pow(1.4, level - 1)),
      prerequisites: level > 5 ? [{ type: 'building', id: 'fortress', level: Math.floor(level / 2) }] : [],
      bonus: `Enables research up to level ${level}`
    });
  }

  return levels;
}

function generateStorageVaultLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    const capacity = Math.floor(5000 * Math.pow(1.5, level - 1));

    levels.push({
      level,
      cost: {
        wood: Math.floor(100 * Math.pow(1.45, level - 1)),
        stone: Math.floor(150 * Math.pow(1.45, level - 1)),
        metal: Math.floor(75 * Math.pow(1.45, level - 1))
      },
      buildTime: Math.floor(60 * Math.pow(1.3, level - 1)),
      prerequisites: [],
      capacity,
      bonus: `Protected storage: ${capacity.toLocaleString()} per resource`
    });
  }

  return levels;
}

function generateTheaterLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 10; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(300 * Math.pow(1.5, level - 1)),
        stone: Math.floor(200 * Math.pow(1.5, level - 1)),
        gold: Math.floor(150 * Math.pow(1.5, level - 1))
      },
      buildTime: Math.floor(120 * Math.pow(1.3, level - 1)),
      prerequisites: [],
      bonus: `+${level * 2}% happiness`
    });
  }

  return levels;
}

function generateMetalsmithLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(150 * Math.pow(1.5, level - 1)),
        stone: Math.floor(100 * Math.pow(1.5, level - 1)),
        metal: Math.floor(200 * Math.pow(1.5, level - 1))
      },
      buildTime: Math.floor(90 * Math.pow(1.3, level - 1)),
      prerequisites: [],
      bonus: `Required for Metallurgy research level ${level}`
    });
  }

  return levels;
}

function generateFactoryLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(200 * Math.pow(1.5, level - 1)),
        stone: Math.floor(150 * Math.pow(1.5, level - 1)),
        metal: Math.floor(100 * Math.pow(1.5, level - 1))
      },
      buildTime: Math.floor(100 * Math.pow(1.3, level - 1)),
      prerequisites: [],
      bonus: level === 1 ? 'Enables trading' : 'Increased production efficiency'
    });
  }

  return levels;
}

function generateRookeryLevels(): BuildingLevel[] {
  const levels: BuildingLevel[] = [];

  for (let level = 1; level <= 35; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(250 * Math.pow(1.6, level - 1)),
        stone: Math.floor(200 * Math.pow(1.6, level - 1)),
        metal: Math.floor(150 * Math.pow(1.6, level - 1))
      },
      buildTime: Math.floor(150 * Math.pow(1.35, level - 1)),
      prerequisites: [],
      bonus: `Dragon management capacity increased`
    });
  }

  return levels;
}

/**
 * Helper function to get building config
 */
export function getBuildingConfig(buildingId: string): BuildingConfig | null {
  return BUILDINGS[buildingId] || null;
}

/**
 * Helper function to get building level config
 */
export function getBuildingLevelConfig(buildingId: string, level: number): BuildingLevel | null {
  const building = BUILDINGS[buildingId];
  if (!building) return null;

  return building.levels.find(l => l.level === level) || null;
}

/**
 * Calculate adjusted build time with Levitation research
 */
export function calculateBuildTime(baseTime: number, levitationLevel: number): number {
  return Math.floor(baseTime / (1 + 0.1 * levitationLevel));
}
