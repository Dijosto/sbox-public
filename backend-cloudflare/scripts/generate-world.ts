/**
 * Script to generate world map SQL data in multiple files
 * Run with: npx tsx scripts/generate-world.ts
 *
 * Generates separate files to avoid wrangler memory issues:
 * - generated-world-camps.sql (all NPC camps)
 * - generated-world-tiles-1.sql through 4.sql (wilderness tiles split)
 */

import { writeFileSync } from 'fs';
import { generateWorldMapSQL } from '../src/utils/worldgen';

const WORLD_SEED = 42069; // Fixed seed for consistent world generation
const WILDERNESS_COUNT = 10000; // Number of wilderness tiles to generate (test-friendly)

console.log('========================================');
console.log('World Map Generation');
console.log('========================================');
console.log('Seed:', WORLD_SEED);
console.log('Generated at:', new Date().toISOString());
console.log('');

// Generate world data
const { campInserts, tileInserts, campCount, wildernessCount } = generateWorldMapSQL(WORLD_SEED, WILDERNESS_COUNT);

console.log('Statistics:');
console.log(`  Anthropus Camps: ${campCount}`);
console.log(`  Wilderness Tiles: ${wildernessCount}`);
console.log(`  Total Tiles: ${campCount + wildernessCount}`);
console.log('');

// Split camps into 2 files for manageable loading
const campStatements = campInserts.split('\n\n').filter(s => s.trim());
const campsPerFile = Math.ceil(campStatements.length / 2);

for (let fileNum = 0; fileNum < 2; fileNum++) {
  const start = fileNum * campsPerFile;
  const end = Math.min(start + campsPerFile, campStatements.length);
  const fileStatements = campStatements.slice(start, end);

  const campsFile = `-- NPC Camps (Part ${fileNum + 1}/2)
-- Generated at: ${new Date().toISOString()}

${fileStatements.join('\n\n')}
`;

  const filename = `generated-world-camps-${fileNum + 1}.sql`;
  writeFileSync(filename, campsFile);
  console.log(`[${fileNum + 1}/4] Writing NPC camps part ${fileNum + 1}...`);
  console.log(`  ✓ ${filename} (${Math.round(campsFile.length / 1024)}KB)`);
}

// Split tiles into 2 files for manageable loading
const tileStatements = tileInserts.split('\n\n').filter(s => s.trim());
const tilesPerFile = Math.ceil(tileStatements.length / 2);

for (let fileNum = 0; fileNum < 2; fileNum++) {
  const start = fileNum * tilesPerFile;
  const end = Math.min(start + tilesPerFile, tileStatements.length);
  const fileStatements = tileStatements.slice(start, end);

  const tilesFile = `-- World Tiles (Part ${fileNum + 1}/2)
-- Generated at: ${new Date().toISOString()}

${fileStatements.join('\n\n')}
`;

  const filename = `generated-world-tiles-${fileNum + 1}.sql`;
  writeFileSync(filename, tilesFile);
  console.log(`[${fileNum + 3}/4] Writing wilderness tiles part ${fileNum + 1}...`);
  console.log(`  ✓ ${filename} (${Math.round(tilesFile.length / 1024)}KB)`);
}

console.log('');
console.log('========================================');
console.log('Generation Complete!');
console.log('========================================');
console.log('');
console.log('Files created:');
console.log('  - generated-world-camps-1.sql');
console.log('  - generated-world-camps-2.sql');
console.log('  - generated-world-tiles-1.sql');
console.log('  - generated-world-tiles-2.sql');
console.log('');
console.log('Next: Run ./setup-world.sh to load into database');
