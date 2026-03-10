/**
 * Dragon system utilities
 * Guardian dragons for combat, stats, and bonuses
 */

/**
 * Great Dragon health by level (from Dragons of Atlantis wiki)
 * Levels 1-2 have 0 HP and cannot fight
 */
const GREAT_DRAGON_HEALTH_BY_LEVEL: Record<number, number> = {
  1: 0,
  2: 0,
  3: 20000,
  4: 45947,
  5: 74743,
  6: 105560,
  7: 137972,
  8: 171716,
  9: 206608,
  10: 242514,
  11: 795656,
  12: 855656,
  13: 930656,
  14: 1090735,
  15: 1270700,
  16: 1500000,
  17: 1500000, // Estimated (wiki missing)
  18: 1500000, // Estimated (wiki missing)
  19: 2250000,
  20: 2500000
};

export interface DragonStats {
  dragonType: string;
  speed: number;
  range: number;
  rangedDamage: number;
  meleeDamage: number;
  defense: number;
  health: number;
  level: number;
  location: 'mainCity' | 'hillsOutpost' | 'mountainsOutpost' | 'lakesOutpost' | 'savannaOutpost' | 'forestOutpost';
  gender: 'male' | 'female';
  specialization: 'balanced' | 'defense' | 'ranged' | 'healing' | 'speed' | 'debuff';
}

export interface DragonInstance {
  dragonId: string;
  dragonType: string;
  level: number;
  currentHealth: number;
  maxHealth: number;
  experience: number;
}

/**
 * Dragon type configurations at level 11 (from Dragons of Atlantis)
 */
const DRAGON_CONFIGS: Record<string, Omit<DragonStats, 'level' | 'health'>> = {
  greatDragon: {
    dragonType: 'greatDragon',
    speed: 1000,
    range: 1900,
    rangedDamage: 9975,
    meleeDamage: 9275,
    defense: 10275,
    location: 'mainCity',
    gender: 'female',
    specialization: 'balanced'
  },
  stoneDragon: {
    dragonType: 'stoneDragon',
    speed: 700,
    range: 1700,
    rangedDamage: 9095,
    meleeDamage: 12125,
    defense: 18187,
    location: 'hillsOutpost',
    gender: 'male',
    specialization: 'defense'
  },
  fireDragon: {
    dragonType: 'fireDragon',
    speed: 950,
    range: 1800,
    rangedDamage: 13750,
    meleeDamage: 9970,
    defense: 6062,
    location: 'mountainsOutpost',
    gender: 'male',
    specialization: 'ranged'
  },
  waterDragon: {
    dragonType: 'waterDragon',
    speed: 850,
    range: 1750,
    rangedDamage: 10500,
    meleeDamage: 10000,
    defense: 9500,
    location: 'lakesOutpost',
    gender: 'male',
    specialization: 'healing'
  },
  frostDragon: {
    dragonType: 'frostDragon',
    speed: 800,
    range: 1700,
    rangedDamage: 11000,
    meleeDamage: 9500,
    defense: 8500,
    location: 'mountainsOutpost',
    gender: 'male',
    specialization: 'debuff'
  },
  windDragon: {
    dragonType: 'windDragon',
    speed: 1100,
    range: 1850,
    rangedDamage: 10800,
    meleeDamage: 9800,
    defense: 8000,
    location: 'savannaOutpost',
    gender: 'male',
    specialization: 'speed'
  },
  battleDragon: {
    dragonType: 'battleDragon',
    speed: 750,
    range: 1500,
    rangedDamage: 300,
    meleeDamage: 300,
    defense: 300,
    location: 'mainCity',
    gender: 'male',
    specialization: 'speed'
  },
  swiftStrikeDragon: {
    dragonType: 'swiftStrikeDragon',
    speed: 900,
    range: 1400,
    rangedDamage: 250,
    meleeDamage: 250,
    defense: 250,
    location: 'mainCity',
    gender: 'male',
    specialization: 'speed'
  }
};

/**
 * Get dragon configuration
 */
export function getDragonConfig(dragonType: string): DragonStats | null {
  const baseConfig = DRAGON_CONFIGS[dragonType];
  if (!baseConfig) return null;

  // Default to level 11 with scaled health
  const level = 11;
  const health = calculateDragonHealth(dragonType, level);

  return {
    ...baseConfig,
    level,
    health
  };
}

/**
 * Calculate dragon health based on level
 * Uses actual Dragons of Atlantis wiki data for Great Dragon
 * Other dragons use estimated scaling formula
 */
export function calculateDragonHealth(dragonType: string, level: number): number {
  // Great Dragon uses actual wiki data
  if (dragonType === 'greatDragon') {
    return GREAT_DRAGON_HEALTH_BY_LEVEL[level] || GREAT_DRAGON_HEALTH_BY_LEVEL[20];
  }

  // Other dragons use estimated formula based on specialization
  // Base health at level 3 (levels 1-2 have 0 HP)
  const baseHealthMap: Record<string, number> = {
    stoneDragon: 24000, // 20% higher (defense specialist)
    fireDragon: 18000, // 10% lower (offense specialist)
    waterDragon: 22000, // 10% higher (healing specialist)
    frostDragon: 20000, // Same as Great Dragon
    windDragon: 19000, // 5% lower (speed specialist)
    battleDragon: 1500,
    swiftStrikeDragon: 1200
  };

  const baseHealth = baseHealthMap[dragonType] || 20000;

  if (level <= 2) return 0; // Levels 1-2 have 0 HP
  if (level === 3) return baseHealth;

  // Scale health similarly to Great Dragon pattern
  // Level 11 is roughly 3.98x level 10 for Great Dragon (795656 / 242514)
  if (level <= 10) {
    // Linear scaling up to level 10
    const growthPerLevel = baseHealth * 0.35;
    return Math.floor(baseHealth + (growthPerLevel * (level - 3)));
  } else {
    // Exponential scaling for levels 11+
    const level10Health = baseHealth + (baseHealth * 0.35 * 7);
    const multiplier = level === 11 ? 3.98 : Math.pow(1.08, level - 11) * 3.98;
    return Math.floor(level10Health * multiplier);
  }
}

/**
 * Calculate health fighting minimum percentage
 * Dragons stop fighting when they reach this health threshold
 *
 * From DoA:
 * - Base: 95% (Aerial Combat Level 1)
 * - Formula: 95 - (aerialCombat * 5)
 * - Max: 50% (Aerial Combat Level 10)
 */
export function calculateHealthFightingMinimum(aerialCombatLevel: number): number {
  if (aerialCombatLevel === 0) {
    return 100; // Dragon won't fight at all without Aerial Combat research
  }

  const minimum = 95 - (aerialCombatLevel * 5);
  return Math.max(50, minimum); // Cap at 50% (level 10+)
}

/**
 * Calculate dragon combat bonus multipliers
 * Dragons provide passive bonuses to troops in the march
 */
export interface DragonCombatBonus {
  attackMultiplier: number;
  defenseMultiplier: number;
  healthMultiplier: number;
}

export function calculateDragonBonus(
  dragonType: string,
  dragonLevel: number
): DragonCombatBonus {
  // Base bonus: 5% per dragon level
  const baseBonus = dragonLevel * 0.05;

  // Specialization modifiers
  const bonusMap: Record<string, DragonCombatBonus> = {
    greatDragon: {
      attackMultiplier: 1 + baseBonus,
      defenseMultiplier: 1 + baseBonus,
      healthMultiplier: 1 + baseBonus
    },
    stoneDragon: {
      attackMultiplier: 1 + baseBonus * 0.7,
      defenseMultiplier: 1 + baseBonus * 1.5, // Defense specialist
      healthMultiplier: 1 + baseBonus * 1.2
    },
    fireDragon: {
      attackMultiplier: 1 + baseBonus * 1.5, // Attack specialist
      defenseMultiplier: 1 + baseBonus * 0.6,
      healthMultiplier: 1 + baseBonus * 0.8
    },
    waterDragon: {
      attackMultiplier: 1 + baseBonus * 0.9,
      defenseMultiplier: 1 + baseBonus * 0.9,
      healthMultiplier: 1 + baseBonus * 1.4 // Health specialist
    },
    frostDragon: {
      attackMultiplier: 1 + baseBonus * 1.1,
      defenseMultiplier: 1 + baseBonus * 0.8,
      healthMultiplier: 1 + baseBonus
    },
    windDragon: {
      attackMultiplier: 1 + baseBonus * 1.2,
      defenseMultiplier: 1 + baseBonus * 0.7,
      healthMultiplier: 1 + baseBonus * 0.9
    },
    battleDragon: {
      attackMultiplier: 1 + baseBonus * 0.8,
      defenseMultiplier: 1 + baseBonus * 0.8,
      healthMultiplier: 1 + baseBonus * 0.8
    },
    swiftStrikeDragon: {
      attackMultiplier: 1 + baseBonus * 0.7,
      defenseMultiplier: 1 + baseBonus * 0.7,
      healthMultiplier: 1 + baseBonus * 0.7
    }
  };

  return bonusMap[dragonType] || {
    attackMultiplier: 1,
    defenseMultiplier: 1,
    healthMultiplier: 1
  };
}

/**
 * Check if dragon can participate in combat based on current health
 */
export function canDragonFight(
  currentHealthPercent: number,
  aerialCombatLevel: number
): boolean {
  const minimum = calculateHealthFightingMinimum(aerialCombatLevel);
  return currentHealthPercent >= minimum;
}

/**
 * Calculate dragon damage in combat
 * Dragons act as powerful troops with high stats
 */
export function calculateDragonDamage(
  dragon: DragonStats,
  isRanged: boolean,
  distance: number
): number {
  if (isRanged && distance <= dragon.range) {
    return dragon.rangedDamage;
  } else {
    return dragon.meleeDamage;
  }
}

/**
 * Calculate dragon healing over time
 * Dragons passively heal when not on marches
 *
 * Healing Rate: 1% of max health per hour
 * Dragons heal faster in Dragon Keep (potential future upgrade)
 */
export function calculateDragonHealing(
  dragon: DragonInstance,
  timeSinceLastUpdate: number // milliseconds
): number {
  if (dragon.currentHealth >= dragon.maxHealth) {
    return 0; // Already at full health
  }

  // Base healing: 1% of max health per hour
  const HEALING_RATE_PER_HOUR = 0.01;
  const hours = timeSinceLastUpdate / (1000 * 60 * 60);
  const healingAmount = Math.floor(dragon.maxHealth * HEALING_RATE_PER_HOUR * hours);

  return healingAmount;
}

/**
 * Apply healing to dragon
 * Returns updated dragon with healed health (capped at max)
 */
export function applyDragonHealing(
  dragon: DragonInstance,
  timeSinceLastUpdate: number
): DragonInstance {
  const healing = calculateDragonHealing(dragon, timeSinceLastUpdate);

  return {
    ...dragon,
    currentHealth: Math.min(dragon.maxHealth, dragon.currentHealth + healing)
  };
}
