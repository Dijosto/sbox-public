/**
 * Game Configuration - Troop Definitions
 * Based on Dragons of Atlantis game data
 */

export interface TroopConfig {
  id: string;
  name: string;
  category: 'transport' | 'melee' | 'ranged' | 'speed' | 'elite';
  stats: TroopStats;
  cost: TroopCost;
  trainingTime: number; // Base time in seconds
  prerequisites: TroopPrerequisite[];
  power: number; // Power value per unit
}

export interface TroopStats {
  meleeAttack: number;
  rangedAttack: number;
  defense: number;
  health: number;
  speed: number;
  range: number;
  loadCapacity: number;
}

export interface TroopCost {
  food: number;
  wood?: number;
  stone?: number;
  metal?: number;
}

export interface TroopPrerequisite {
  type: 'building' | 'research';
  id: string;
  level: number;
}

/**
 * Troop Definitions
 */
export const TROOPS: Record<string, TroopConfig> = {
  // Transport Troops
  porter: {
    id: 'porter',
    name: 'Porter',
    category: 'transport',
    stats: {
      meleeAttack: 1,
      rangedAttack: 0,
      defense: 10,
      health: 45,
      speed: 100,
      range: 0,
      loadCapacity: 200
    },
    cost: {
      food: 50,
      wood: 10
    },
    trainingTime: 30,
    prerequisites: [],
    power: 0.5
  },

  armoredTransport: {
    id: 'armoredTransport',
    name: 'Armored Transport',
    category: 'transport',
    stats: {
      meleeAttack: 5,
      rangedAttack: 0,
      defense: 150,
      health: 500,
      speed: 200,
      range: 0,
      loadCapacity: 500
    },
    cost: {
      food: 200,
      wood: 50,
      metal: 50
    },
    trainingTime: 120,
    prerequisites: [
      { type: 'research', id: 'militaryScience', level: 3 }
    ],
    power: 2
  },

  // Melee Troops
  conscript: {
    id: 'conscript',
    name: 'Conscript',
    category: 'melee',
    stats: {
      meleeAttack: 10,
      rangedAttack: 0,
      defense: 10,
      health: 75,
      speed: 200,
      range: 0,
      loadCapacity: 50
    },
    cost: {
      food: 30
    },
    trainingTime: 30,
    prerequisites: [],
    power: 1
  },

  halberdsman: {
    id: 'halberdsman',
    name: 'Halberdsman',
    category: 'melee',
    stats: {
      meleeAttack: 40,
      rangedAttack: 0,
      defense: 40,
      health: 150,
      speed: 300,
      range: 0,
      loadCapacity: 75
    },
    cost: {
      food: 100,
      wood: 25
    },
    trainingTime: 60,
    prerequisites: [
      { type: 'building', id: 'garrison', level: 3 }
    ],
    power: 3
  },

  giant: {
    id: 'giant',
    name: 'Giant',
    category: 'melee',
    stats: {
      meleeAttack: 1000,
      rangedAttack: 0,
      defense: 400,
      health: 4000,
      speed: 120,
      range: 0,
      loadCapacity: 500
    },
    cost: {
      food: 5000,
      wood: 1000,
      stone: 1000,
      metal: 500
    },
    trainingTime: 3600, // 1 hour
    prerequisites: [
      { type: 'research', id: 'clairvoyance', level: 3 },
      { type: 'building', id: 'garrison', level: 10 }
    ],
    power: 50
  },

  // Ranged Troops
  longbowman: {
    id: 'longbowman',
    name: 'Longbowman',
    category: 'ranged',
    stats: {
      meleeAttack: 5,
      rangedAttack: 80,
      defense: 30,
      health: 75,
      speed: 250,
      range: 1200,
      loadCapacity: 50
    },
    cost: {
      food: 80,
      wood: 40
    },
    trainingTime: 90,
    prerequisites: [
      { type: 'research', id: 'weaponsCalibration', level: 1 },
      { type: 'building', id: 'garrison', level: 2 }
    ],
    power: 4
  },

  crossbowman: {
    id: 'crossbowman',
    name: 'Crossbowman',
    category: 'ranged',
    stats: {
      meleeAttack: 10,
      rangedAttack: 120,
      defense: 50,
      health: 100,
      speed: 200,
      range: 1000,
      loadCapacity: 60
    },
    cost: {
      food: 150,
      wood: 75,
      metal: 25
    },
    trainingTime: 150,
    prerequisites: [
      { type: 'research', id: 'weaponsCalibration', level: 3 },
      { type: 'building', id: 'garrison', level: 5 }
    ],
    power: 6
  },

  // Speed Troops
  battleDragon: {
    id: 'battleDragon',
    name: 'Battle Dragon',
    category: 'speed',
    stats: {
      meleeAttack: 300,
      rangedAttack: 300,
      defense: 300,
      health: 1500,
      speed: 750,
      range: 1500,
      loadCapacity: 1000
    },
    cost: {
      food: 10000,
      wood: 2000,
      stone: 2000,
      metal: 1000
    },
    trainingTime: 7200, // 2 hours
    prerequisites: [
      { type: 'research', id: 'dragonry', level: 3 },
      { type: 'building', id: 'rookery', level: 5 }
    ],
    power: 100
  },

  swiftStrikeDragon: {
    id: 'swiftStrikeDragon',
    name: 'Swift Strike Dragon',
    category: 'speed',
    stats: {
      meleeAttack: 250,
      rangedAttack: 200,
      defense: 200,
      health: 1000,
      speed: 900,
      range: 1200,
      loadCapacity: 800
    },
    cost: {
      food: 8000,
      wood: 1500,
      stone: 1500,
      metal: 800
    },
    trainingTime: 5400, // 1.5 hours
    prerequisites: [
      { type: 'research', id: 'dragonry', level: 2 },
      { type: 'building', id: 'rookery', level: 3 }
    ],
    power: 80
  },

  // Elite Troops (examples)
  lavaJaws: {
    id: 'lavaJaws',
    name: 'Lava Jaws',
    category: 'elite',
    stats: {
      meleeAttack: 50,
      rangedAttack: 500,
      defense: 200,
      health: 800,
      speed: 350,
      range: 1500,
      loadCapacity: 200
    },
    cost: {
      food: 1000,
      wood: 300,
      stone: 200,
      metal: 400
    },
    trainingTime: 1800, // 30 minutes
    prerequisites: [
      { type: 'building', id: 'trainingCamp', level: 10 },
      { type: 'research', id: 'advancedMilitary', level: 5 }
    ],
    power: 8
  }
};

/**
 * Helper function to get troop config
 */
export function getTroopConfig(troopId: string): TroopConfig | null {
  return TROOPS[troopId] || null;
}

/**
 * Calculate adjusted training time based on garrison levels
 */
export function calculateTrainingTime(
  baseTroopTime: number,
  quantity: number,
  garrisonLevels: number[],
  garrisonCount: number
): number {
  // Base formula: time = (base_time * quantity) / training_speed_multiplier
  // Training speed scales with garrison count and average level

  const avgGarrisonLevel = garrisonLevels.reduce((a, b) => a + b, 0) / garrisonLevels.length;
  const speedMultiplier = 1 + (garrisonCount * 0.1) + (avgGarrisonLevel * 0.05);

  const totalTime = (baseTroopTime * quantity) / speedMultiplier;

  // Minimum 30 seconds
  return Math.max(30, Math.floor(totalTime));
}

/**
 * Calculate food consumption for troops
 * Formula: base_consumption * quantity * (1 - rationing_bonus)
 */
export function calculateFoodConsumption(
  troopId: string,
  quantity: number,
  rationingLevel: number
): number {
  const troop = TROOPS[troopId];
  if (!troop) return 0;

  // Base consumption: 1 food per hour per troop (simplified)
  const baseConsumption = quantity;

  // Rationing research: -5% per level, max 100% at level 20
  const rationingBonus = Math.min(1.0, rationingLevel * 0.05);

  return Math.floor(baseConsumption * (1 - rationingBonus));
}

/**
 * Calculate combat stats with research bonuses
 */
export function calculateCombatStats(
  troopId: string,
  quantity: number,
  metallurgyLevel: number,
  medicineLevel: number
): {
  totalMeleeAttack: number;
  totalRangedAttack: number;
  totalDefense: number;
  totalHealth: number;
} {
  const troop = TROOPS[troopId];
  if (!troop) {
    return {
      totalMeleeAttack: 0,
      totalRangedAttack: 0,
      totalDefense: 0,
      totalHealth: 0
    };
  }

  // Metallurgy: +5% attack/defense per level (±10% at L11+)
  const metallurgyBonus = metallurgyLevel <= 10
    ? 1 + (metallurgyLevel * 0.05)
    : 1 + (10 * 0.05) + ((metallurgyLevel - 10) * 0.10);

  // Medicine: +5% health per level (±10% at L11+)
  const medicineBonus = medicineLevel <= 10
    ? 1 + (medicineLevel * 0.05)
    : 1 + (10 * 0.05) + ((medicineLevel - 10) * 0.10);

  return {
    totalMeleeAttack: Math.floor(troop.stats.meleeAttack * quantity * metallurgyBonus),
    totalRangedAttack: Math.floor(troop.stats.rangedAttack * quantity * metallurgyBonus),
    totalDefense: Math.floor(troop.stats.defense * quantity * metallurgyBonus),
    totalHealth: Math.floor(troop.stats.health * quantity * medicineBonus)
  };
}
