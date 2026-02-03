/**
 * Combat resolution system
 * Deterministic combat calculations inspired by Dragons of Atlantis
 */

import { getTroopConfig } from './troops';

export interface CombatTroop {
  troopType: string;
  quantity: number;
}

export interface CombatSide {
  playerId: string;
  playerName: string;
  troops: CombatTroop[];
  research: Record<string, number>; // Research bonuses
  dragonBonus?: number; // Dragon attack/defense bonus
}

export interface CombatResult {
  winner: 'attacker' | 'defender' | 'draw';
  attackerLosses: CombatTroop[];
  defenderLosses: CombatTroop[];
  attackerSurvivors: CombatTroop[];
  defenderSurvivors: CombatTroop[];
  rounds: CombatRound[];
  loot?: {
    food: number;
    wood: number;
    stone: number;
    metal: number;
    gold: number;
  };
}

export interface CombatRound {
  round: number;
  attackerDamage: number;
  defenderDamage: number;
  attackerCasualties: CombatTroop[];
  defenderCasualties: CombatTroop[];
}

/**
 * Resolve combat between attacker and defender
 */
export function resolveCombat(
  attacker: CombatSide,
  defender: CombatSide
): CombatResult {
  // Deep copy troop arrays to avoid modifying originals
  let attackerTroops = JSON.parse(JSON.stringify(attacker.troops)) as CombatTroop[];
  let defenderTroops = JSON.parse(JSON.stringify(defender.troops)) as CombatTroop[];

  const rounds: CombatRound[] = [];
  const maxRounds = 10; // Maximum combat rounds

  // Calculate research bonuses
  const attackerAttackBonus = calculateAttackBonus(attacker.research) + (attacker.dragonBonus || 0);
  const attackerDefenseBonus = calculateDefenseBonus(attacker.research) + (attacker.dragonBonus || 0);
  const attackerHealthBonus = calculateHealthBonus(attacker.research);
  const defenderAttackBonus = calculateAttackBonus(defender.research) + (defender.dragonBonus || 0);
  const defenderDefenseBonus = calculateDefenseBonus(defender.research) + (defender.dragonBonus || 0);
  const defenderHealthBonus = calculateHealthBonus(defender.research);

  // Combat rounds
  for (let round = 1; round <= maxRounds; round++) {
    // Check if either side is eliminated
    if (getTotalTroops(attackerTroops) === 0 || getTotalTroops(defenderTroops) === 0) {
      break;
    }

    // Calculate total damage from each side
    const attackerDamage = calculateTotalDamage(attackerTroops, attackerAttackBonus, 'attack');
    const defenderDamage = calculateTotalDamage(defenderTroops, defenderAttackBonus, 'defense');

    // Apply damage and calculate casualties
    const defenderCasualties = applyDamage(defenderTroops, attackerDamage, defenderDefenseBonus, defenderHealthBonus);
    const attackerCasualties = applyDamage(attackerTroops, defenderDamage, attackerDefenseBonus, attackerHealthBonus);

    rounds.push({
      round,
      attackerDamage,
      defenderDamage,
      attackerCasualties,
      defenderCasualties
    });
  }

  // Determine winner
  const attackerRemaining = getTotalTroops(attackerTroops);
  const defenderRemaining = getTotalTroops(defenderTroops);

  let winner: 'attacker' | 'defender' | 'draw';
  if (attackerRemaining > 0 && defenderRemaining === 0) {
    winner = 'attacker';
  } else if (defenderRemaining > 0 && attackerRemaining === 0) {
    winner = 'defender';
  } else {
    winner = 'draw';
  }

  // Calculate losses
  const attackerLosses = calculateLosses(attacker.troops, attackerTroops);
  const defenderLosses = calculateLosses(defender.troops, defenderTroops);

  return {
    winner,
    attackerLosses,
    defenderLosses,
    attackerSurvivors: attackerTroops,
    defenderSurvivors: defenderTroops,
    rounds
  };
}

/**
 * Calculate attack bonus from research
 */
function calculateAttackBonus(research: Record<string, number>): number {
  let bonus = 0;

  // Metallurgy: +5% per level (levels 1-10), +10% per level (11-20)
  const metallurgy = research['metallurgy'] || 0;
  if (metallurgy > 0) {
    bonus += metallurgy <= 10 ? metallurgy * 5 : 50 + (metallurgy - 10) * 10;
  }

  // Conscription: +troop attack
  const conscription = research['conscription'] || 0;
  bonus += conscription * 5;

  return bonus;
}

/**
 * Calculate defense bonus from research
 */
function calculateDefenseBonus(research: Record<string, number>): number {
  let bonus = 0;

  // Metallurgy: +5% per level (levels 1-10), +10% per level (11-20)
  const metallurgy = research['metallurgy'] || 0;
  if (metallurgy > 0) {
    bonus += metallurgy <= 10 ? metallurgy * 5 : 50 + (metallurgy - 10) * 10;
  }

  // Fortification: +defense
  const fortification = research['fortification'] || 0;
  bonus += fortification * 5;

  return bonus;
}

/**
 * Calculate health bonus from research
 */
function calculateHealthBonus(research: Record<string, number>): number {
  let bonus = 0;

  // Medicine: +5% per level (levels 1-10), +10% per level (11-20)
  const medicine = research['medicine'] || 0;
  if (medicine > 0) {
    bonus += medicine <= 10 ? medicine * 5 : 50 + (medicine - 10) * 10;
  }

  return bonus;
}

/**
 * Calculate total damage output from a troop stack
 */
function calculateTotalDamage(
  troops: CombatTroop[],
  bonus: number,
  phase: 'attack' | 'defense'
): number {
  let totalDamage = 0;

  for (const troop of troops) {
    const config = getTroopConfig(troop.troopType);
    if (!config) continue;

    // Use ranged attack for attack phase, melee for defense
    const baseDamage = phase === 'attack'
      ? Math.max(config.stats.rangedAttack, config.stats.meleeAttack)
      : config.stats.meleeAttack;

    const damageWithBonus = baseDamage * (1 + bonus / 100);
    totalDamage += damageWithBonus * troop.quantity;
  }

  return totalDamage;
}

/**
 * Apply damage to troops and return casualties
 */
function applyDamage(
  troops: CombatTroop[],
  damage: number,
  defenseBonus: number,
  healthBonus: number
): CombatTroop[] {
  const casualties: CombatTroop[] = [];
  let remainingDamage = damage;

  // Apply damage to troops (weakest first)
  const sortedTroops = [...troops].sort((a, b) => {
    const configA = getTroopConfig(a.troopType);
    const configB = getTroopConfig(b.troopType);
    return (configA?.stats.health || 0) - (configB?.stats.health || 0);
  });

  for (const troop of sortedTroops) {
    if (remainingDamage <= 0 || troop.quantity === 0) continue;

    const config = getTroopConfig(troop.troopType);
    if (!config) continue;

    // Calculate effective health with defense and health bonuses
    const effectiveHealth = config.stats.health * (1 + defenseBonus / 100) * (1 + healthBonus / 100);

    // Calculate how many troops die
    const troopsKilled = Math.min(
      troop.quantity,
      Math.floor(remainingDamage / effectiveHealth)
    );

    if (troopsKilled > 0) {
      casualties.push({
        troopType: troop.troopType,
        quantity: troopsKilled
      });

      // Update troop count
      const troopIndex = troops.findIndex(t => t.troopType === troop.troopType);
      if (troopIndex !== -1) {
        troops[troopIndex].quantity -= troopsKilled;
      }

      remainingDamage -= troopsKilled * effectiveHealth;
    }
  }

  return casualties;
}

/**
 * Get total troop count
 */
function getTotalTroops(troops: CombatTroop[]): number {
  return troops.reduce((sum, troop) => sum + troop.quantity, 0);
}

/**
 * Calculate losses by comparing original and remaining troops
 */
function calculateLosses(
  original: CombatTroop[],
  remaining: CombatTroop[]
): CombatTroop[] {
  const losses: CombatTroop[] = [];

  for (const originalTroop of original) {
    const remainingTroop = remaining.find(t => t.troopType === originalTroop.troopType);
    const lost = originalTroop.quantity - (remainingTroop?.quantity || 0);

    if (lost > 0) {
      losses.push({
        troopType: originalTroop.troopType,
        quantity: lost
      });
    }
  }

  return losses;
}

/**
 * Calculate loot from successful attack
 */
export function calculateLoot(
  defenderResources: {
    food: number;
    wood: number;
    stone: number;
    metal: number;
    gold: number;
  },
  attackerCapacity: number,
  victorySeverity: number // 0-1, based on how decisive the victory was
): {
  food: number;
  wood: number;
  stone: number;
  metal: number;
  gold: number;
} {
  // Base loot percentage based on victory severity (10% - 50%)
  const lootPercentage = 0.1 + (victorySeverity * 0.4);

  // Calculate available loot
  const availableLoot = {
    food: Math.floor(defenderResources.food * lootPercentage),
    wood: Math.floor(defenderResources.wood * lootPercentage),
    stone: Math.floor(defenderResources.stone * lootPercentage),
    metal: Math.floor(defenderResources.metal * lootPercentage),
    gold: Math.floor(defenderResources.gold * lootPercentage)
  };

  // Total loot amount
  const totalLoot = Object.values(availableLoot).reduce((sum, val) => sum + val, 0);

  // If total loot exceeds capacity, scale it down proportionally
  if (totalLoot > attackerCapacity) {
    const scale = attackerCapacity / totalLoot;
    return {
      food: Math.floor(availableLoot.food * scale),
      wood: Math.floor(availableLoot.wood * scale),
      stone: Math.floor(availableLoot.stone * scale),
      metal: Math.floor(availableLoot.metal * scale),
      gold: Math.floor(availableLoot.gold * scale)
    };
  }

  return availableLoot;
}
