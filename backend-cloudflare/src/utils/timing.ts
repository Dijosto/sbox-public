/**
 * Timing utilities for game events
 * Handles test mode speed multipliers
 */

import { Env } from '../workers/index';

/**
 * Get the test speed multiplier from environment
 * Returns 1 (normal speed) in production
 * Returns configured multiplier in local dev only
 *
 * SECURITY: This must only be set via server environment variables,
 * never controllable by client requests
 */
export function getTestSpeedMultiplier(env: Env): number {
  // Only allow in local development
  if (!env.TEST_SPEED_MULTIPLIER) {
    return 1;
  }

  const multiplier = parseInt(env.TEST_SPEED_MULTIPLIER, 10);

  // Validate it's a reasonable number
  if (isNaN(multiplier) || multiplier < 1 || multiplier > 1000) {
    console.warn('[Timing] Invalid TEST_SPEED_MULTIPLIER, using 1x speed');
    return 1;
  }

  console.log(`[Timing] Using ${multiplier}x speed multiplier for testing`);
  return multiplier;
}

/**
 * Apply test speed multiplier to a duration in seconds
 * Returns the adjusted duration
 */
export function applySpeedMultiplier(durationSeconds: number, env: Env): number {
  const multiplier = getTestSpeedMultiplier(env);
  return Math.max(1, Math.floor(durationSeconds / multiplier));
}

/**
 * Calculate time until completion timestamp
 * Applies test speed multiplier
 */
export function calculateCompletionTime(durationSeconds: number, env: Env): number {
  const adjustedDuration = applySpeedMultiplier(durationSeconds, env);
  return Date.now() + (adjustedDuration * 1000);
}
