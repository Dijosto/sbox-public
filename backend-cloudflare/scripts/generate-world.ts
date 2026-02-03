/**
 * Script to generate world map SQL data
 * Run with: npx tsx scripts/generate-world.ts > generated-world.sql
 */

import { generateWorldMapSQL } from '../src/utils/worldgen';

const WORLD_SEED = 42069; // Fixed seed for consistent world generation

console.log('-- Generated World Map for Dragons of Atlantis Clone');
console.log('-- Seed:', WORLD_SEED);
console.log('-- Generated at:', new Date().toISOString());
console.log('');

const { campInserts, tileInserts, campCount, wildernessCount } = generateWorldMapSQL(WORLD_SEED);

console.log('-- Statistics:');
console.log(`--   Anthropus Camps: ${campCount}`);
console.log(`--   Wilderness Tiles: ${wildernessCount}`);
console.log(`--   Total Tiles: ${campCount + wildernessCount}`);
console.log('');

console.log('-- ==========================================');
console.log('-- NPC Camps');
console.log('-- ==========================================');
console.log(campInserts);
console.log('');

console.log('-- ==========================================');
console.log('-- World Tiles');
console.log('-- ==========================================');
console.log(tileInserts);
