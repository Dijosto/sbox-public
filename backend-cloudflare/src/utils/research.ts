/**
 * Game Configuration - Research Definitions
 * Based on Dragons of Atlantis game data
 */

export interface ResearchConfig {
  id: string;
  name: string;
  category: 'production' | 'military' | 'speed' | 'economic' | 'dragon';
  maxLevel: number;
  levels: ResearchLevel[];
}

export interface ResearchLevel {
  level: number;
  cost: ResearchCost;
  researchTime: number; // seconds
  prerequisites: ResearchPrerequisite[];
  bonus: string;
  effect: ResearchEffect;
}

export interface ResearchCost {
  food?: number;
  wood?: number;
  stone?: number;
  metal?: number;
  gold?: number;
}

export interface ResearchPrerequisite {
  type: 'building' | 'research';
  id: string;
  level: number;
}

export interface ResearchEffect {
  type: 'production' | 'combat' | 'speed' | 'consumption' | 'unlock';
  value: number; // Percentage or flat bonus
  target?: string; // What it affects
}

/**
 * Research Definitions
 */
export const RESEARCH: Record<string, ResearchConfig> = {
  // Resource Production
  agriculture: {
    id: 'agriculture',
    name: 'Agriculture',
    category: 'production',
    maxLevel: 20,
    levels: generateProductionResearch('agriculture', 'food', 10, 25)
  },

  woodcraft: {
    id: 'woodcraft',
    name: 'Woodcraft',
    category: 'production',
    maxLevel: 20,
    levels: generateProductionResearch('woodcraft', 'wood', 10, 20)
  },

  masonry: {
    id: 'masonry',
    name: 'Masonry',
    category: 'production',
    maxLevel: 20,
    levels: generateProductionResearch('masonry', 'stone', 10, 20)
  },

  alloys: {
    id: 'alloys',
    name: 'Alloys',
    category: 'production',
    maxLevel: 20,
    levels: generateProductionResearch('alloys', 'metal', 10, 20)
  },

  // Military Combat
  metallurgy: {
    id: 'metallurgy',
    name: 'Metallurgy',
    category: 'military',
    maxLevel: 20,
    levels: generateMetallurgyLevels()
  },

  medicine: {
    id: 'medicine',
    name: 'Medicine',
    category: 'military',
    maxLevel: 20,
    levels: generateMedicineLevels()
  },

  weaponsCalibration: {
    id: 'weaponsCalibration',
    name: 'Weapons Calibration',
    category: 'military',
    maxLevel: 20,
    levels: generateWeaponsCalibrationLevels()
  },

  // Dragon Research
  dragonry: {
    id: 'dragonry',
    name: 'Dragonry',
    category: 'dragon',
    maxLevel: 20,
    levels: generateDragonyLevels()
  },

  aerialCombat: {
    id: 'aerialCombat',
    name: 'Aerial Combat',
    category: 'dragon',
    maxLevel: 20,
    levels: generateAerialCombatLevels()
  },

  // Speed & Efficiency
  levitation: {
    id: 'levitation',
    name: 'Levitation',
    category: 'speed',
    maxLevel: 20,
    levels: generateLevitationLevels()
  },

  rapidDeployment: {
    id: 'rapidDeployment',
    name: 'Rapid Deployment',
    category: 'speed',
    maxLevel: 20,
    levels: generateRapidDeploymentLevels()
  },

  // Economic
  rationing: {
    id: 'rationing',
    name: 'Rationing',
    category: 'economic',
    maxLevel: 20,
    levels: generateRationingLevels()
  },

  mercantilism: {
    id: 'mercantilism',
    name: 'Mercantilism',
    category: 'economic',
    maxLevel: 16,
    levels: generateMercantilismLevels()
  },

  clairvoyance: {
    id: 'clairvoyance',
    name: 'Clairvoyance',
    category: 'economic',
    maxLevel: 10,
    levels: generateClairvoyanceLevels()
  }
};

/**
 * Helper functions to generate research level data
 */

function generateProductionResearch(
  id: string,
  resource: string,
  bonusL1_10: number,
  bonusL11Plus: number
): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const bonus = level <= 10 ? bonusL1_10 : bonusL11Plus;

    levels.push({
      level,
      cost: {
        food: Math.floor(500 * Math.pow(1.6, level - 1)),
        wood: Math.floor(300 * Math.pow(1.6, level - 1)),
        stone: Math.floor(200 * Math.pow(1.6, level - 1)),
        gold: Math.floor(100 * Math.pow(1.5, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level === 1 ? [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ] : [
        { type: 'research', id, level: level - 1 }
      ],
      bonus: `+${bonus}% ${resource} production`,
      effect: {
        type: 'production',
        value: bonus,
        target: resource
      }
    });
  }

  return levels;
}

function generateMetallurgyLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const bonus = level <= 10 ? 5 : 10;

    levels.push({
      level,
      cost: {
        food: Math.floor(800 * Math.pow(1.7, level - 1)),
        metal: Math.floor(400 * Math.pow(1.7, level - 1)),
        gold: Math.floor(200 * Math.pow(1.6, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: [
        { type: 'research', id: 'alloys', level },
        { type: 'building', id: 'metalsmith', level },
        { type: 'building', id: 'garrison', level }
      ],
      bonus: `+${bonus}% attack and defense`,
      effect: {
        type: 'combat',
        value: bonus,
        target: 'attack_defense'
      }
    });
  }

  return levels;
}

function generateMedicineLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const bonus = level <= 10 ? 5 : 10;

    levels.push({
      level,
      cost: {
        food: Math.floor(600 * Math.pow(1.65, level - 1)),
        wood: Math.floor(400 * Math.pow(1.65, level - 1)),
        gold: Math.floor(150 * Math.pow(1.55, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'medicine', level: level - 1 },
        { type: 'building', id: 'scienceCenter', level: Math.max(6, Math.ceil(level / 2)) }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 6 }
      ],
      bonus: `+${bonus}% troop health`,
      effect: {
        type: 'combat',
        value: bonus,
        target: 'health'
      }
    });
  }

  return levels;
}

function generateWeaponsCalibrationLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    levels.push({
      level,
      cost: {
        wood: Math.floor(700 * Math.pow(1.65, level - 1)),
        metal: Math.floor(500 * Math.pow(1.65, level - 1)),
        gold: Math.floor(200 * Math.pow(1.6, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'weaponsCalibration', level: level - 1 }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ],
      bonus: `+${5 + level}% ranged damage`,
      effect: {
        type: 'combat',
        value: 5 + level,
        target: 'ranged_damage'
      }
    });
  }

  return levels;
}

function generateDragonyLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    let unlocks = '';
    if (level === 2) unlocks = ' (Unlocks Swift Strike Dragons)';
    if (level === 3) unlocks = ' (Unlocks Battle Dragons)';

    levels.push({
      level,
      cost: {
        food: Math.floor(1000 * Math.pow(1.7, level - 1)),
        wood: Math.floor(800 * Math.pow(1.7, level - 1)),
        stone: Math.floor(600 * Math.pow(1.7, level - 1)),
        metal: Math.floor(400 * Math.pow(1.7, level - 1)),
        gold: Math.floor(300 * Math.pow(1.65, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'dragonry', level: level - 1 }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ],
      bonus: `+5% dragon speed${unlocks}`,
      effect: {
        type: 'combat',
        value: 5,
        target: 'dragon_speed'
      }
    });
  }

  return levels;
}

function generateAerialCombatLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const minHealth = Math.max(0, 95 - (level * 5));

    levels.push({
      level,
      cost: {
        food: Math.floor(1500 * Math.pow(1.75, level - 1)),
        gold: Math.floor(500 * Math.pow(1.7, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: [
        { type: 'research', id: 'dragonry', level: Math.ceil(level / 2) }
      ],
      bonus: `Dragon minimum health: ${minHealth}%`,
      effect: {
        type: 'combat',
        value: minHealth,
        target: 'dragon_min_health'
      }
    });
  }

  return levels;
}

function generateLevitationLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const speedBonus = Math.floor((level * 10 / (1 + 0.1 * level)) * 100) / 100;

    levels.push({
      level,
      cost: {
        wood: Math.floor(1000 * Math.pow(1.7, level - 1)),
        stone: Math.floor(800 * Math.pow(1.7, level - 1)),
        gold: Math.floor(300 * Math.pow(1.65, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level === 1 ? [
        { type: 'research', id: 'woodcraft', level: 5 }
      ] : [
        { type: 'research', id: 'levitation', level: level - 1 }
      ],
      bonus: `${speedBonus.toFixed(1)}% faster building`,
      effect: {
        type: 'speed',
        value: level * 10, // Used in formula: 1 / (1 + 0.1 * level)
        target: 'building_speed'
      }
    });
  }

  return levels;
}

function generateRapidDeploymentLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    levels.push({
      level,
      cost: {
        food: Math.floor(800 * Math.pow(1.65, level - 1)),
        wood: Math.floor(600 * Math.pow(1.65, level - 1)),
        gold: Math.floor(250 * Math.pow(1.6, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'rapidDeployment', level: level - 1 }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ],
      bonus: `+5% march speed`,
      effect: {
        type: 'speed',
        value: 5,
        target: 'march_speed'
      }
    });
  }

  return levels;
}

function generateRationingLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 20; level++) {
    const reduction = level * 5;

    levels.push({
      level,
      cost: {
        food: Math.floor(500 * Math.pow(1.6, level - 1)),
        gold: Math.floor(200 * Math.pow(1.55, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'rationing', level: level - 1 }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ],
      bonus: `-${reduction}% food consumption`,
      effect: {
        type: 'consumption',
        value: reduction,
        target: 'food'
      }
    });
  }

  return levels;
}

function generateMercantilismLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 16; level++) {
    levels.push({
      level,
      cost: {
        food: Math.floor(600 * Math.pow(1.6, level - 1)),
        wood: Math.floor(400 * Math.pow(1.6, level - 1)),
        gold: Math.floor(300 * Math.pow(1.6, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level === 1 ? [
        { type: 'building', id: 'factory', level: 1 }
      ] : [
        { type: 'research', id: 'mercantilism', level: level - 1 }
      ],
      bonus: `+1 trade slot (total: ${level})`,
      effect: {
        type: 'unlock',
        value: 1,
        target: 'trade_slots'
      }
    });
  }

  return levels;
}

function generateClairvoyanceLevels(): ResearchLevel[] {
  const levels: ResearchLevel[] = [];

  for (let level = 1; level <= 10; level++) {
    let unlocks = '';
    if (level === 1) unlocks = ' (Unlocks Spies)';
    if (level === 3) unlocks = ' (Unlocks Giants)';

    levels.push({
      level,
      cost: {
        food: Math.floor(800 * Math.pow(1.65, level - 1)),
        gold: Math.floor(400 * Math.pow(1.65, level - 1))
      },
      researchTime: calculateResearchTime(level),
      prerequisites: level > 1 ? [
        { type: 'research', id: 'clairvoyance', level: level - 1 }
      ] : [
        { type: 'building', id: 'scienceCenter', level: 1 }
      ],
      bonus: `Intelligence capacity increased${unlocks}`,
      effect: {
        type: 'unlock',
        value: level,
        target: 'intelligence'
      }
    });
  }

  return levels;
}

/**
 * Calculate research time based on level
 * Early: minutes to hours
 * Mid (6-10): hours to 2-3 days
 * High (11-15): 5-7 days
 * Very high (16-20): 7+ days
 */
function calculateResearchTime(level: number): number {
  if (level <= 5) {
    // 5 minutes to 2 hours
    return Math.floor(300 * Math.pow(1.8, level - 1));
  } else if (level <= 10) {
    // 2 hours to 3 days
    return Math.floor(7200 * Math.pow(1.4, level - 5));
  } else if (level <= 15) {
    // 3 days to 7 days
    return Math.floor(259200 * Math.pow(1.2, level - 10));
  } else {
    // 7+ days
    return Math.floor(604800 * Math.pow(1.15, level - 15));
  }
}

/**
 * Helper function to get research config
 */
export function getResearchConfig(researchId: string): ResearchConfig | null {
  return RESEARCH[researchId] || null;
}

/**
 * Helper function to get research level config
 */
export function getResearchLevelConfig(researchId: string, level: number): ResearchLevel | null {
  const research = RESEARCH[researchId];
  if (!research) return null;

  return research.levels.find(l => l.level === level) || null;
}

/**
 * Check if prerequisites are met for research
 */
export function checkResearchPrerequisites(
  research: ResearchLevel,
  playerState: any
): { valid: boolean; reason?: string } {
  for (const prereq of research.prerequisites) {
    if (prereq.type === 'building') {
      // Search for building by type (e.g., "scienceCenter" matches "science_center_1")
      let foundBuilding = null;
      let maxLevel = 0;

      // Check inner city
      for (const [buildingId, building] of Object.entries(playerState.city.innerCity)) {
        if (building.buildingType === prereq.id) {
          maxLevel = Math.max(maxLevel, building.level);
          foundBuilding = building;
        }
      }

      // Check outer fields
      for (const [buildingId, building] of Object.entries(playerState.city.outerFields)) {
        if (building.buildingType === prereq.id) {
          maxLevel = Math.max(maxLevel, building.level);
          foundBuilding = building;
        }
      }

      if (!foundBuilding || maxLevel < prereq.level) {
        return {
          valid: false,
          reason: `Requires ${prereq.id} level ${prereq.level}`
        };
      }
    } else if (prereq.type === 'research') {
      const currentLevel = playerState.research[prereq.id] || 0;
      if (currentLevel < prereq.level) {
        return {
          valid: false,
          reason: `Requires ${prereq.id} level ${prereq.level}`
        };
      }
    }
  }

  return { valid: true };
}
