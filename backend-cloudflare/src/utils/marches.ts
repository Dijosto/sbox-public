/**
 * March system utilities
 * Handles march creation, travel time calculation, and combat coordination
 */

import { getTroopConfig } from './troops';

export interface MarchTroops {
  troopType: string;
  quantity: number;
}

export interface March {
  marchId: string;
  playerId: string;
  playerName: string;
  origin: { x: number; y: number };
  destination: { x: number; y: number };
  troops: MarchTroops[];
  marchType: 'attack' | 'gather' | 'scout' | 'reinforce' | 'transport';
  departureTime: number;
  arrivalTime: number;
  returnTime?: number;
  resources?: {
    food?: number;
    wood?: number;
    stone?: number;
    metal?: number;
    gold?: number;
  };
  status: 'outbound' | 'at_target' | 'returning' | 'completed';
  targetType: 'player' | 'npc' | 'wilderness';
  targetId?: string; // Player ID or NPC camp ID
}

/**
 * Calculate distance between two coordinates
 */
export function calculateDistance(
  from: { x: number; y: number },
  to: { x: number; y: number }
): number {
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  return Math.sqrt(dx * dx + dy * dy);
}

/**
 * Calculate march travel time based on distance and troop speed
 * @param distance - Distance in map units
 * @param troops - Array of troop types and quantities
 * @param speedBonus - Speed bonus from research (percentage)
 * @returns Travel time in seconds
 */
export function calculateMarchTime(
  distance: number,
  troops: MarchTroops[],
  speedBonus: number = 0
): number {
  // Find slowest troop in the march
  let slowestSpeed = Infinity;

  for (const troop of troops) {
    const troopConfig = getTroopConfig(troop.troopType);
    if (troopConfig && troopConfig.stats.speed < slowestSpeed) {
      slowestSpeed = troopConfig.stats.speed;
    }
  }

  // Base speed is the slowest troop
  // Speed stat represents map units per hour
  const effectiveSpeed = slowestSpeed * (1 + speedBonus / 100);

  // Calculate hours needed, convert to seconds
  const hours = distance / effectiveSpeed;
  const seconds = hours * 3600;

  // Minimum march time: 10 seconds
  return Math.max(10, Math.floor(seconds));
}

/**
 * Calculate load capacity of troops in march
 */
export function calculateMarchCapacity(troops: MarchTroops[]): number {
  let totalCapacity = 0;

  for (const troop of troops) {
    const troopConfig = getTroopConfig(troop.troopType);
    if (troopConfig) {
      totalCapacity += troopConfig.stats.loadCapacity * troop.quantity;
    }
  }

  return totalCapacity;
}

/**
 * Validate if a march is possible
 */
export function validateMarch(
  troops: MarchTroops[],
  availableTroops: { troopType: string; quantity: number }[],
  origin: { x: number; y: number },
  destination: { x: number; y: number }
): { valid: boolean; error?: string } {
  // Check if troops are provided
  if (troops.length === 0) {
    return { valid: false, error: 'No troops provided' };
  }

  // Check if player has enough troops
  for (const marchTroop of troops) {
    const available = availableTroops.find(t => t.troopType === marchTroop.troopType);
    if (!available || available.quantity < marchTroop.quantity) {
      return {
        valid: false,
        error: `Insufficient ${marchTroop.troopType} (need ${marchTroop.quantity}, have ${available?.quantity || 0})`
      };
    }
  }

  // Check if origin and destination are different
  if (origin.x === destination.x && origin.y === destination.y) {
    return { valid: false, error: 'Cannot march to same location' };
  }

  // Check if march distance is within limits (max 1000 units)
  const distance = calculateDistance(origin, destination);
  if (distance > 1000) {
    return { valid: false, error: 'Target too far away' };
  }

  return { valid: true };
}

/**
 * Calculate power of a troop stack
 */
export function calculateMarchPower(troops: MarchTroops[]): number {
  let totalPower = 0;

  for (const troop of troops) {
    const troopConfig = getTroopConfig(troop.troopType);
    if (troopConfig) {
      totalPower += troopConfig.power * troop.quantity;
    }
  }

  return totalPower;
}
