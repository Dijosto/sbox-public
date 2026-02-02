/**
 * Resource Production and Economy Calculations
 * Based on Dragons of Atlantis formulas
 */

export interface Resources {
  food: number;
  wood: number;
  stone: number;
  metal: number;
  gold: number;
  premiumCurrency: number;
}

export interface ProductionRates {
  foodRate: number;
  woodRate: number;
  stoneRate: number;
  metalRate: number;
  goldRate: number;
}

export interface ResourceCaps {
  foodCap: number;
  woodCap: number;
  stoneCap: number;
  metalCap: number;
  goldCap: number;
}

/**
 * Calculate resource production rates
 * Formula: base * (1 + research_bonus) * (1 + wilderness_bonus)
 */
export function calculateProductionRates(
  buildings: Record<string, { level: number }>,
  research: Record<string, number>,
  wilderness: Record<string, number>
): ProductionRates {
  // Get research levels
  const agricultureLevel = research['agriculture'] || 0;
  const woodcraftLevel = research['woodcraft'] || 0;
  const masonryLevel = research['masonry'] || 0;
  const alloysLevel = research['alloys'] || 0;

  // Calculate research bonuses
  const agriBonus = calculateResearchBonus(agricultureLevel, 10, 25);
  const woodBonus = calculateResearchBonus(woodcraftLevel, 10, 20);
  const stoneBonus = calculateResearchBonus(masonryLevel, 10, 20);
  const metalBonus = calculateResearchBonus(alloysLevel, 10, 20);

  // Calculate wilderness bonuses (5% per level)
  const wildernessFood = (wilderness['lakes'] || 0) * 0.05;
  const wildernessWood = (wilderness['forests'] || 0) * 0.05;
  const wildernessStone = (wilderness['hills'] || 0) * 0.05;
  const wildernessMetal = (wilderness['mountains'] || 0) * 0.05;

  // Calculate base production from buildings
  let baseFood = 0;
  let baseWood = 0;
  let baseStone = 0;
  let baseMetal = 0;

  // Sum up all farms, lumbermills, quarries, mines
  Object.entries(buildings).forEach(([id, building]) => {
    if (id.startsWith('farm')) {
      baseFood += calculateBuildingProduction(building.level);
    } else if (id.startsWith('lumbermill')) {
      baseWood += calculateBuildingProduction(building.level);
    } else if (id.startsWith('quarry')) {
      baseStone += calculateBuildingProduction(building.level);
    } else if (id.startsWith('mine')) {
      baseMetal += calculateBuildingProduction(building.level);
    }
  });

  // Apply bonuses
  return {
    foodRate: Math.floor(baseFood * (1 + agriBonus) * (1 + wildernessFood)),
    woodRate: Math.floor(baseWood * (1 + woodBonus) * (1 + wildernessWood)),
    stoneRate: Math.floor(baseStone * (1 + stoneBonus) * (1 + wildernessStone)),
    metalRate: Math.floor(baseMetal * (1 + metalBonus) * (1 + wildernessMetal)),
    goldRate: 0 // Calculated separately from taxation
  };
}

/**
 * Calculate research bonus
 */
function calculateResearchBonus(level: number, bonusL1_10: number, bonusL11Plus: number): number {
  if (level === 0) return 0;

  let total = 0;

  // Levels 1-10
  const levelsBelow10 = Math.min(level, 10);
  total += levelsBelow10 * (bonusL1_10 / 100);

  // Levels 11+
  if (level > 10) {
    total += (level - 10) * (bonusL11Plus / 100);
  }

  return total;
}

/**
 * Calculate building production based on level
 * Formula: 100 * 1.3^(level - 1)
 */
function calculateBuildingProduction(level: number): number {
  return Math.floor(100 * Math.pow(1.3, level - 1));
}

/**
 * Calculate gold generation from taxation
 * Formula: home_capacity * (happiness / 100) * (tax_rate / 100)
 */
export function calculateGoldProduction(
  homeCapacity: number,
  taxRate: number,
  theaterLevel: number
): number {
  // Calculate happiness
  // Formula: 100 - tax_rate + (theater_level * 2)
  const happiness = Math.max(0, Math.min(120, 100 - taxRate + (theaterLevel * 2)));

  // Calculate hourly gold
  return Math.floor(homeCapacity * (happiness / 100) * (taxRate / 100));
}

/**
 * Calculate population capacity
 * Formula: actual_population = max_capacity * (happiness / 100)
 */
export function calculatePopulation(
  maxCapacity: number,
  taxRate: number,
  theaterLevel: number
): number {
  const happiness = Math.max(0, Math.min(120, 100 - taxRate + (theaterLevel * 2)));
  return Math.floor(maxCapacity * (happiness / 100));
}

/**
 * Calculate home capacity from buildings
 */
export function calculateHomeCapacity(buildings: Record<string, { level: number }>): number {
  let capacity = 0;

  Object.entries(buildings).forEach(([id, building]) => {
    if (id.startsWith('home')) {
      // Formula: 100 * 1.4^(level - 1)
      capacity += Math.floor(100 * Math.pow(1.4, building.level - 1));
    }
  });

  return capacity;
}

/**
 * Calculate resource storage caps
 */
export function calculateStorageCaps(buildings: Record<string, { level: number }>): ResourceCaps {
  // Base storage from resource buildings
  let baseFood = 5000;
  let baseWood = 5000;
  let baseStone = 5000;
  let baseMetal = 5000;
  let baseGold = 1000;

  // Add storage from farms, lumbermills, etc. (10% of production as storage)
  Object.entries(buildings).forEach(([id, building]) => {
    const level = building.level;

    if (id.startsWith('farm')) {
      baseFood += Math.floor(calculateBuildingProduction(level) * 0.1);
    } else if (id.startsWith('lumbermill')) {
      baseWood += Math.floor(calculateBuildingProduction(level) * 0.1);
    } else if (id.startsWith('quarry')) {
      baseStone += Math.floor(calculateBuildingProduction(level) * 0.1);
    } else if (id.startsWith('mine')) {
      baseMetal += Math.floor(calculateBuildingProduction(level) * 0.1);
    }
  });

  // Add storage vault capacity
  let vaultCapacity = 0;
  Object.entries(buildings).forEach(([id, building]) => {
    if (id === 'storageVault') {
      // Formula: 5000 * 1.5^(level - 1)
      vaultCapacity = Math.floor(5000 * Math.pow(1.5, building.level - 1));
    }
  });

  return {
    foodCap: baseFood + vaultCapacity,
    woodCap: baseWood + vaultCapacity,
    stoneCap: baseStone + vaultCapacity,
    metalCap: baseMetal + vaultCapacity,
    goldCap: baseGold + Math.floor(vaultCapacity * 0.2)
  };
}

/**
 * Calculate accumulated resources over time
 */
export function calculateAccumulatedResources(
  currentResources: Resources,
  productionRates: ProductionRates,
  caps: ResourceCaps,
  elapsedHours: number
): Resources {
  return {
    food: Math.min(caps.foodCap, Math.floor(currentResources.food + productionRates.foodRate * elapsedHours)),
    wood: Math.min(caps.woodCap, Math.floor(currentResources.wood + productionRates.woodRate * elapsedHours)),
    stone: Math.min(caps.stoneCap, Math.floor(currentResources.stone + productionRates.stoneRate * elapsedHours)),
    metal: Math.min(caps.metalCap, Math.floor(currentResources.metal + productionRates.metalRate * elapsedHours)),
    gold: Math.min(caps.goldCap, Math.floor(currentResources.gold + productionRates.goldRate * elapsedHours)),
    premiumCurrency: currentResources.premiumCurrency // Never auto-generated
  };
}

/**
 * Check if player has enough resources for a cost
 */
export function hasEnoughResources(
  resources: Resources,
  cost: {
    food?: number;
    wood?: number;
    stone?: number;
    metal?: number;
    gold?: number;
  }
): { valid: boolean; missing?: string } {
  if (cost.food && resources.food < cost.food) {
    return { valid: false, missing: `food (need ${cost.food}, have ${resources.food})` };
  }
  if (cost.wood && resources.wood < cost.wood) {
    return { valid: false, missing: `wood (need ${cost.wood}, have ${resources.wood})` };
  }
  if (cost.stone && resources.stone < cost.stone) {
    return { valid: false, missing: `stone (need ${cost.stone}, have ${resources.stone})` };
  }
  if (cost.metal && resources.metal < cost.metal) {
    return { valid: false, missing: `metal (need ${cost.metal}, have ${resources.metal})` };
  }
  if (cost.gold && resources.gold < cost.gold) {
    return { valid: false, missing: `gold (need ${cost.gold}, have ${resources.gold})` };
  }

  return { valid: true };
}

/**
 * Deduct resources
 */
export function deductResources(
  resources: Resources,
  cost: {
    food?: number;
    wood?: number;
    stone?: number;
    metal?: number;
    gold?: number;
  }
): Resources {
  return {
    food: resources.food - (cost.food || 0),
    wood: resources.wood - (cost.wood || 0),
    stone: resources.stone - (cost.stone || 0),
    metal: resources.metal - (cost.metal || 0),
    gold: resources.gold - (cost.gold || 0),
    premiumCurrency: resources.premiumCurrency
  };
}
