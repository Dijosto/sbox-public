/**
 * NPC Camp utilities
 * Handles NPC camp regeneration and strength calculations
 */

/**
 * Calculate current NPC camp strength based on regeneration
 *
 * Regeneration Rules (from DoA):
 * - 15 minute initial respawn delay after defeat
 * - 10% regeneration every 5 minutes after initial delay
 * - Full respawn takes 50 minutes from 0% to 100%
 * - Total time: 65 minutes (15 min delay + 50 min regen)
 *
 * @param lastDefeated - Timestamp when camp was last defeated (0 if never defeated)
 * @param currentStrength - Current strength percentage from database
 * @returns Updated strength percentage (0-100)
 */
export function calculateNPCStrength(
  lastDefeated: number | null,
  currentStrength: number
): number {
  // If never defeated or already at full strength, return 100%
  if (!lastDefeated || currentStrength >= 100) {
    return 100;
  }

  const now = Date.now();
  const timeSinceDefeat = now - lastDefeated;

  // Constants (in milliseconds)
  const INITIAL_RESPAWN_DELAY = 15 * 60 * 1000; // 15 minutes
  const REGEN_INTERVAL = 5 * 60 * 1000; // 5 minutes
  const REGEN_PER_INTERVAL = 10; // 10% per interval

  // If still in initial respawn delay, strength remains at 0
  if (timeSinceDefeat < INITIAL_RESPAWN_DELAY) {
    return 0;
  }

  // Calculate time since regeneration started
  const timeRegenning = timeSinceDefeat - INITIAL_RESPAWN_DELAY;

  // Calculate number of full 5-minute intervals that have passed
  const intervalsCompleted = Math.floor(timeRegenning / REGEN_INTERVAL);

  // Calculate new strength (10% per interval, max 100%)
  const newStrength = Math.min(100, intervalsCompleted * REGEN_PER_INTERVAL);

  return newStrength;
}

/**
 * Calculate NPC camp garrison based on current strength percentage
 *
 * @param fullGarrison - Full strength garrison (JSON object with troop types)
 * @param strengthPercent - Current strength percentage (0-100)
 * @returns Scaled garrison with troop quantities adjusted
 */
export function scaleNPCGarrison(
  fullGarrison: Record<string, { quantity: number }>,
  strengthPercent: number
): Record<string, { quantity: number }> {
  const scaledGarrison: Record<string, { quantity: number }> = {};
  const multiplier = strengthPercent / 100;

  for (const [troopType, data] of Object.entries(fullGarrison)) {
    scaledGarrison[troopType] = {
      quantity: Math.floor(data.quantity * multiplier)
    };
  }

  return scaledGarrison;
}

/**
 * Calculate NPC camp loot based on current strength percentage
 *
 * @param fullResources - Full resources available at 100% strength
 * @param strengthPercent - Current strength percentage (0-100)
 * @returns Scaled resources
 */
export function scaleNPCResources(
  fullResources: Record<string, number>,
  strengthPercent: number
): Record<string, number> {
  const scaledResources: Record<string, number> = {};
  const multiplier = strengthPercent / 100;

  for (const [resource, amount] of Object.entries(fullResources)) {
    scaledResources[resource] = Math.floor(amount * multiplier);
  }

  return scaledResources;
}

/**
 * Update NPC camp after battle
 *
 * @param survivingTroops - Troops that survived the battle
 * @param fullGarrison - Full strength garrison
 * @returns New strength percentage
 */
export function calculatePostBattleStrength(
  survivingTroops: Record<string, { quantity: number }>,
  fullGarrison: Record<string, { quantity: number }>
): number {
  // Calculate total surviving troops vs full garrison
  let totalSurviving = 0;
  let totalFull = 0;

  for (const [troopType, data] of Object.entries(fullGarrison)) {
    totalFull += data.quantity;
    totalSurviving += survivingTroops[troopType]?.quantity || 0;
  }

  if (totalFull === 0) return 0;

  return Math.floor((totalSurviving / totalFull) * 100);
}
