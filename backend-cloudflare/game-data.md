# Dragons of Atlantis - Complete Game Data Reference

This document contains all extracted game mechanics, formulas, and data from the Dragons of Atlantis fandom wiki to implement an accurate server-authoritative version.

---

## TABLE OF CONTENTS

1. [Buildings System](#buildings-system)
2. [Troops & Combat](#troops--combat)
3. [Resources & Economy](#resources--economy)
4. [Research/Tech Tree](#researchtech-tree)
5. [Dragons System](#dragons-system)
6. [World Map](#world-map)
7. [Alliances](#alliances)
8. [Formulas](#formulas)
9. [Progression Systems](#progression-systems)
10. [Implementation Notes](#implementation-notes)

---

## BUILDINGS SYSTEM

### Building Categories

**Core Indestructible Structures**
- Fortress (Main defensive structure, gates other buildings)
- Wall (Perimeter defense)
- Dragon Keep (Dragon storage/breeding)
- Defensive Tower (Active defense system)

**Resource Production**
```json
{
  "farm": {
    "produces": "food",
    "maxLevel": 35,
    "requirements": [],
    "capacity": {
      "level1": 1000,
      "level35": 100000
    }
  },
  "lumbermill": {
    "produces": "wood",
    "maxLevel": 35,
    "requirements": []
  },
  "quarry": {
    "produces": "stone",
    "maxLevel": 35,
    "requirements": []
  },
  "mine": {
    "produces": "metal",
    "maxLevel": 35,
    "requirements": []
  }
}
```

**Military Infrastructure**
```json
{
  "garrison": {
    "purpose": "Troop training",
    "maxLevel": 35,
    "requirements": [],
    "trainingSpeed": "scales with level and quantity"
  },
  "musterPoint": {
    "purpose": "Army assembly/march capacity",
    "maxLevel": 30,
    "marchCapacity": {
      "level30": 1160000
    }
  },
  "sentinel": {
    "purpose": "Spy defense",
    "maxLevel": 10,
    "effect": "Determines spy information visibility"
  }
}
```

**Support Structures**
```json
{
  "home": {
    "purpose": "Population housing",
    "maxLevel": 35,
    "capacity": {
      "level16": 1000,
      "level35": 100000
    }
  },
  "theater": {
    "purpose": "Happiness bonus",
    "maxLevel": 10,
    "bonusPerLevel": 2,
    "maxBonus": 20
  },
  "storageVault": {
    "purpose": "Resource protection from raids",
    "maxLevel": 35
  },
  "scienceCenter": {
    "purpose": "Research facility",
    "maxLevel": 35,
    "requirements": []
  },
  "factory": {
    "purpose": "Trading + resource production",
    "maxLevel": 35,
    "unlocks": {
      "level1": "Trading enabled"
    }
  },
  "metalsmith": {
    "purpose": "Resource processing",
    "maxLevel": 35,
    "requirements": ["Required for Metallurgy research"]
  },
  "rookery": {
    "purpose": "Dragon management",
    "maxLevel": 35
  },
  "officersQuarters": {
    "purpose": "General recruitment",
    "maxLevel": 35
  }
}
```

**Building Upgrade Rules**
- Levels 1-10: Standard progression
- Level 10+: Requires Completion Grants
- Level 11+: Requires Ancestral Seals (one per level)
- Most buildings max at level 35 in main city

---

## TROOPS & COMBAT

### Troop Types & Stats

**Transport Troops**
```json
{
  "porter": {
    "type": "transport",
    "meleeAttack": 1,
    "defense": 10,
    "health": 45,
    "speed": 100,
    "range": 0,
    "loadCapacity": 200,
    "trainingCost": {
      "food": 50,
      "wood": 10,
      "time": 30
    }
  },
  "armoredTransport": {
    "type": "transport",
    "defense": "high",
    "health": "high",
    "speed": "medium",
    "range": 0,
    "loadCapacity": "high",
    "note": "Optimal meat shield for ranged units"
  }
}
```

**Melee Troops**
```json
{
  "conscript": {
    "type": "melee",
    "meleeAttack": 10,
    "defense": 10,
    "health": 75,
    "speed": 200,
    "range": 0,
    "trainingCost": {
      "food": 30,
      "time": 30
    }
  },
  "halberdsman": {
    "type": "melee",
    "meleeAttack": 40,
    "defense": 40,
    "health": 150,
    "speed": 300,
    "range": 0
  },
  "giant": {
    "type": "melee_elite",
    "meleeAttack": 1000,
    "defense": 400,
    "health": 4000,
    "speed": 120,
    "range": 0,
    "requirements": ["Clairvoyance Level 3"]
  }
}
```

**Ranged Troops**
```json
{
  "longbowman": {
    "type": "ranged",
    "rangedAttack": 80,
    "meleeAttack": 5,
    "defense": 30,
    "health": 75,
    "speed": 250,
    "range": 1200,
    "note": "Damage drops to 5 in melee range, further reduced by enemy defense"
  },
  "lavaJaws": {
    "type": "ranged_elite",
    "rangedAttack": "very_high",
    "defense": "medium",
    "health": "high",
    "speed": "medium",
    "range": "long",
    "requirements": ["Fire Outpost", "Training Camp Level 10"],
    "power": 8
  }
}
```

**Speed Troops**
```json
{
  "battleDragon": {
    "type": "speed_dragon",
    "meleeAttack": 300,
    "rangedAttack": 300,
    "defense": 300,
    "health": 1500,
    "speed": 750,
    "range": 1500,
    "requirements": ["Dragonry Level 3"]
  },
  "swiftStrikeDragon": {
    "type": "speed_dragon",
    "meleeAttack": "high",
    "defense": "medium",
    "health": "medium",
    "speed": "very_high",
    "requirements": ["Dragonry Level 2"]
  }
}
```

### Combat Mechanics

**Core Combat Rules**
```javascript
// Initiative
attacker_gets_initiative = true;

// Turn Order
turn_order = sort_by_speed_descending(all_troops);
// If speed tied: attacker goes first

// Action Sequence per Turn
for (troop_group in turn_order) {
  // 1. Check range
  if (enemies_in_range()) {
    attack();
  } else {
    move_forward();
  }
}

// Damage Calculation
effective_damage = base_damage - enemy_defense;
// Troops take no losses until ~50% total health damage accumulated

// Ranged Penalty
if (enemy_in_melee_range) {
  ranged_damage = melee_attack; // Typically 1/16th of ranged damage
}
```

**Battlefield Distance**
```javascript
starting_distance = max_attacker_range + 600;
// Pure melee: 0 + 600 = 600
// With Longbowman: 1200 + 600 = 1800
```

**Optimal Compositions**
- **Camp Farming**: Pure ranged OR pure speed melee (NEVER mix)
- **Meat Shield Strategy**: Armored Transports + Ranged units
- **PvP**: Depends on enemy composition (ranged vs melee ratio)

**Targeting Priorities**
- Dragons target opposing dragons and bowmen
- Each troop type has specific priority system (not random)
- Melee troops block ranged effectiveness

---

## RESOURCES & ECONOMY

### Resource Types

**Primary Resources**
```json
{
  "food": {
    "production": "farms",
    "consumption": "troop upkeep",
    "tradeable": true
  },
  "wood": {
    "production": "lumbermills",
    "consumption": "construction, research",
    "tradeable": true
  },
  "stone": {
    "production": "quarries",
    "consumption": "construction, research",
    "tradeable": true
  },
  "metal": {
    "production": "mines",
    "consumption": "equipment, research",
    "tradeable": true
  },
  "gold": {
    "production": "taxation",
    "consumption": "research, generals, trading fees",
    "tradeable": true
  },
  "rubies": {
    "production": "premium_currency",
    "consumption": "speedups, special items",
    "tradeable": false
  }
}
```

**Special Resources**
```json
{
  "blueEnergy": {
    "production": "Spectral Ruins only",
    "tradeable": false,
    "note": "Non-transferable between cities"
  },
  "talismans": {
    "production": "Anthropus kills (1 per 500)",
    "requirement": "Spectral Ruins unlock",
    "tradeable": false
  },
  "dragonEggs": {
    "production": "wilderness drops, shops",
    "tradeable": false
  },
  "dragonArmor": {
    "production": "wilderness level 5+ drops",
    "types": ["helmet", "breastplate", "gauntlets", "greaves"],
    "tradeable": false
  }
}
```

### Economic Formulas

**Population System**
```javascript
// Population Capacity
actual_population = max_capacity * (happiness / 100);

// Happiness Calculation
happiness = 100 - tax_rate + (theater_level * 2);
// Min: 0%, Max: 120% (with level 10 theater at 0% tax)

// Gold Generation
hourly_gold = home_capacity * (happiness / 100) * (tax_rate / 100);

// Optimal Taxation
optimal_tax_rate = 50; // When happiness = tax rate
// Example: 50% tax + 50% happiness = maximum gold efficiency
```

**Resource Production**
```javascript
// Base Production
base_production = field_level_base_rate;

// Research Multiplier
production_multiplier = 1 + (research_level * 0.10); // +10% per level
// Levels 11+: varies (Agriculture +25%, others +20%)

// Wilderness Bonus
wilderness_bonus = wilderness_level * 0.05; // 5% per level, max 50%

// Total Production
total_production = base_production * production_multiplier * (1 + wilderness_bonus);
```

**Trading System**
```json
{
  "requirements": {
    "factory": 1,
    "levitation": 1,
    "mercantilism": 1
  },
  "duration": 1800,
  "tradeSlots": "mercantilism_level",
  "maxSlots": 16,
  "fee": "proportional_to_resources",
  "priceVariability": "realm_dependent"
}
```

---

## RESEARCH/TECH TREE

### Research Categories

**Resource Production (20 levels each)**
```json
{
  "agriculture": {
    "bonus": "+10% food per level (±25% L11+)",
    "requirements": ["Farm Level 1"],
    "unlocks": {
      "level10": "Passing Troops"
    }
  },
  "woodcraft": {
    "bonus": "+10% lumber per level (±20% L11+)",
    "requirements": ["Lumbermill Level 1"],
    "prerequisites": ["Required for Levitation"]
  },
  "masonry": {
    "bonus": "+10% stone per level (±20% L11+)",
    "requirements": ["Quarry Level 1"]
  },
  "alloys": {
    "bonus": "+10% metal per level (±20% L11+)",
    "requirements": ["Mine Level 1"],
    "prerequisites": ["Required for Metallurgy"]
  }
}
```

**Military Combat (20 levels each)**
```json
{
  "metallurgy": {
    "bonus": "+5% attack/defense per level (±10% L11+)",
    "requirements": ["Alloys matching level", "Metalsmith matching level", "Garrison matching level"],
    "note": "Most important combat research"
  },
  "medicine": {
    "bonus": "+5% troop health per level (±10% L11+)",
    "requirements": ["Science Center Level 6+"],
    "note": "Prevents damage, more valuable than post-damage healing"
  },
  "weaponsCalibration": {
    "bonus": "Ranged damage boost",
    "critical": "Essential for ranged units"
  },
  "dragonry": {
    "bonus": "+5% Battle Dragon/Swift Strike Dragon speed per level",
    "unlocks": {
      "level2": "Swift Strike Dragons",
      "level3": "Battle Dragons"
    }
  },
  "aerialCombat": {
    "bonus": "-5% dragon minimum health per level",
    "requirements": ["Complete Great Dragon Armor set"],
    "maxLevels": 20,
    "effect": {
      "level1": "95% min health",
      "level5": "75% min health",
      "level10": "50% min health"
    }
  }
}
```

**Speed & Efficiency (20 levels each)**
```json
{
  "levitation": {
    "bonus": "-10% building time per level",
    "formula": "adjusted_time = base_time / (1 + 0.1 * level)",
    "requirements": ["Woodcraft Level 5", "Scrollcraft Level 5"],
    "speedImprovement": {
      "level1": "9.09%",
      "level20": "66.67%"
    }
  },
  "rapidDeployment": {
    "bonus": "+5% march speed per level",
    "formula": "See march time calculation in Formulas section"
  },
  "rationing": {
    "bonus": "-5% food consumption per level",
    "maxEffect": "Level 20 = 100% reduction (zero food cost)",
    "stackable": "Can exceed 100% with Sanctuary boosts"
  }
}
```

**Economic & Intelligence**
```json
{
  "mercantilism": {
    "bonus": "+1 trade slot per level",
    "maxLevel": 16,
    "maxSlots": 16
  },
  "clairvoyance": {
    "unlocks": {
      "level1": "Spies",
      "level3": "Giants"
    },
    "maxLevel": 10
  }
}
```

**Research Time Scaling**
- Early levels (1-5): Minutes to hours
- Mid levels (6-10): Hours to 2-3 days
- High levels (11-15): 5-7 days
- Very high (16-20): 7+ days
- **Total progression time**: ~312 days 10 hours for all researches

---

## DRAGONS SYSTEM

### Dragon Types

**Guardian Dragons (Combat)**
```json
{
  "greatDragon": {
    "location": "Main city (always female)",
    "speed": 1000,
    "range": 1900,
    "rangedDamage": 9975,
    "meleeDamage": 9275,
    "defense": 10275,
    "level": 11,
    "breedable": true
  },
  "stoneDragon": {
    "location": "Hills Outpost (male)",
    "speed": 700,
    "range": 1700,
    "rangedDamage": 9095,
    "meleeDamage": 12125,
    "defense": 18187,
    "level": 11,
    "specialization": "Defense"
  },
  "fireDragon": {
    "location": "Mountains Outpost (male)",
    "speed": 950,
    "range": 1800,
    "rangedDamage": 13750,
    "meleeDamage": 9970,
    "defense": 6062,
    "level": 11,
    "specialization": "Ranged damage"
  },
  "waterDragon": {
    "location": "Lakes Outpost (male)",
    "specialization": "Healing + stacking attack"
  },
  "frostDragon": {
    "location": "Mountains Outpost (male)",
    "specialization": "Speed reduction + freeze"
  },
  "windDragon": {
    "location": "Savannas Outpost (male)",
    "specialization": "Speed + ranged absorption"
  }
}
```

**Dragon Battle Arts (Examples at Max Level)**

**Water Dragon**
```json
{
  "eternalSprings": {
    "effect": "Stacking attack boost",
    "maxBonus": "45M at level 11+"
  },
  "waterOfLife": {
    "effect": "Healing based on troop losses",
    "maxHealing": "241M"
  },
  "tidalWave": {
    "effect": "Multi-unit damage",
    "maxDamage": "9.1M"
  }
}
```

**Fire Dragon**
```json
{
  "flameThrowers": {
    "effect": "First-turn ranged boost",
    "maxBoost": "1.8M"
  },
  "moltenRising": {
    "effect": "Single-target strike",
    "maxDamage": "3.78M at level 15"
  },
  "immolation": {
    "effect": "Random troop elimination",
    "scaling": "10%-100%"
  }
}
```

**Frost Dragon**
```json
{
  "icyBlast": {
    "effect": "50% speed reduction + 15% freeze chance"
  },
  "frostBite": {
    "effect": "25-point ranged damage reduction (50 for melee)"
  },
  "blizzard": {
    "damage": "5.46M",
    "multiplier": "5x vs frozen units"
  }
}
```

### Dragon Mechanics

**Combat Rules**
```javascript
// Health Fighting Minimum (with Aerial Combat research)
min_health_percent = 95 - (aerial_combat_level * 5);
// Level 1: 95%
// Level 5: 75%
// Level 10: 50%

// Great & Guardian dragons can attack below 100% health
// Other dragons have different requirements
```

**Breeding System**
```json
{
  "requirements": {
    "maleLevel": 8,
    "femaleLevel": 8,
    "mainCityDragon": "always_female",
    "outpostDragons": "always_male"
  },
  "nonBreedable": ["Wraith", "Kaiser", "Chrono", "Steelshard"],
  "incubationTimes": {
    "common": "5 hours",
    "lesser": "8 hours",
    "heightened": "12 hours",
    "royal": "12 hours",
    "exalted": "24 hours",
    "omniscient": "24+ hours",
    "legendary": "24+ hours",
    "divine": "40 hours"
  }
}
```

**Sanctuary Dragons (Non-combat, passive bonuses)**
```json
{
  "purpose": "Passive ability boosts",
  "combatCapable": false,
  "abilitySlots": {
    "hatch": 1,
    "level6": 2,
    "level10": 3,
    "level11": 4
  },
  "level11Requirement": "Elder Dragon Seal",
  "femaleDragons": "Extremely rare (Sanctuary eggs or events)"
}
```

---

## WORLD MAP

### Map Structure

**Map Dimensions**
- Coordinates: 0-749 (X and Y)
- Wraps at boundaries
- Isometric layout (Y-axis diagonal)
- Distance: Pythagorean calculation

**Wilderness Types**
```json
{
  "lakes": {
    "resource": "food",
    "dragonEggs": ["Water Dragon (L5+)", "Mephitic Serpent (L5+)"],
    "armor": []
  },
  "hills": {
    "resource": "stone",
    "dragonEggs": ["Stone Dragon (L7+)"],
    "armor": ["Stone Dragon armor (L7+)"]
  },
  "mountains": {
    "resource": "metal",
    "dragonEggs": ["Fire Dragon (L7+)", "Frost Dragon (L7+)"],
    "armor": ["Fire armor (L7+)", "Frost armor (L7+)"]
  },
  "forests": {
    "resource": "wood",
    "dragonEggs": ["Amber Crest Dragon (L7+)"],
    "armor": ["Amber Crest armor (L7+)"]
  },
  "savannas": {
    "resource": "food",
    "dragonEggs": ["Wind Dragon (L7+)"],
    "armor": ["Wind armor (L7+)"]
  },
  "plains": {
    "resource": "food/gold",
    "dragonEggs": ["Helio Dragon (L8+)", "Chrono Dragon (L8+)"],
    "armor": ["Helio armor (L8+)", "Chrono armor (L8+)"],
    "outposts": "Only tile type for outpost placement"
  }
}
```

**Wilderness Mechanics**
```javascript
// Production Bonus
bonus_percent = wilderness_level * 5; // 5% per level, max 50% at L10

// Loot Scaling (with Great/Elemental Dragons)
talismans = wilderness_level * 10 + 1;
// Level 1: 1 talisman
// Level 10: 101 talismans

// Regeneration
regeneration_rate = 10; // 10% per 5 minutes
full_respawn_time = 50; // minutes (from 0% to 100%)
initial_respawn = 15; // minutes after first attack
```

**NPC Camps - Anthropus Forces**
```json
{
  "levels": {
    "level1": "Cannibals",
    "level10": "Ragers"
  },
  "drops": ["Dragon eggs", "Armor pieces", "Wraith Dragon remains"],
  "eliteTroopItems": "Chance on attack",
  "farmingStrategy": {
    "clearWaves": "10K Longbowmen + 200 attackers",
    "waving": "Single-troop waves after 2 clear attacks",
    "timing": "All waves land within 10 seconds",
    "regeneration": "10% every 5 minutes"
  },
  "waveLimit": "Server rate-limit: 1-hour ban if exceeded"
}
```

**Crimson Citadels**
```json
{
  "locations": [
    {"x": 250, "y": 456},
    {"x": 295, "y": 140},
    {"x": 501, "y": 20},
    {"x": 700, "y": 724}
  ],
  "quantity": 4,
  "defense": "Level 10 Wall + Atlantean troops",
  "troops": {
    "event": "~9M troops",
    "standard": "~16.8M troops"
  },
  "respawn": "3 minutes"
}
```

---

## ALLIANCES

### Alliance Hierarchy

```json
{
  "overlord": {
    "rank": 1,
    "permissions": ["Full control", "Member management", "Diplomatic stances", "Broadcast"],
    "title": "Leader"
  },
  "lord": {
    "rank": 2,
    "permissions": ["Near-identical to Overlord", "Kick/add members", "Set stance"],
    "title": "Senior Officer"
  },
  "leader": {
    "rank": 3,
    "permissions": ["Recruit/remove members", "Broadcast", "Change stances"],
    "title": "Officer"
  },
  "vassal": {
    "rank": 4,
    "permissions": ["Individual messaging only"],
    "title": "Regular Member"
  }
}
```

### Alliance Features

**Cooperative Mechanics**
- Send reinforcement troops to defend allies
- Resource trading (millions possible between members)
- Shared alliance chat
- Visual diplomatic indicators (green=neutral, red=hostile, blue=friendly)

**Alliance Ranking**
```javascript
// Ranking Formula
alliance_power = sum(member_power);
// Quality > Quantity
// 100 members @ 10M = 1B total power
// 2 members @ 500M = 1B total power (ranked equally)
```

**Coordinated Gameplay**
- Wave attacks on wilderness/camps
- Defensive reinforcement coordination
- Resource support during early game
- Relationship status affects diplomatic interactions

---

## FORMULAS

### Movement & Speed

**March Time Calculation**
```javascript
// S = march time (seconds)
// D = distance (map grid squares)
// B = troop base speed
// RD = Rapid Deployment research level
// Dy = Dragonry research level

S = 31 + (4426.8 * D) / (B * (1 + 0.05 * RD + 0.05 * Dy));

// Example:
// Distance: 100 squares
// Base speed: 250 (Longbowmen)
// RD Level 5, Dragonry Level 3
// S = 31 + (442680) / (250 * (1 + 0.25 + 0.15))
// S = 31 + 442680 / 350
// S = 31 + 1264.8 = 1295.8 seconds (~21.6 minutes)
```

### Construction Time

**Levitation Adjustment**
```javascript
// A = adjusted time
// B = base time
// RL = Levitation research level

A = B / (1 + 0.1 * RL);

// Speed Improvements:
// Level 1: 9.09% faster (divide by 1.1)
// Level 5: 33.33% faster (divide by 1.5)
// Level 10: 50% faster (divide by 2.0)
// Level 20: 66.67% faster (divide by 3.0)
```

### Population Economics

**Population & Gold System**
```javascript
// Population Capacity
actual_population = max_capacity * (happiness / 100);

// Happiness Calculation
happiness = 100 - tax_rate + (theater_level * 2);
// Clamped: 0 <= happiness <= 120

// Hourly Gold Generation
gold_per_hour = home_capacity * (happiness / 100) * (tax_rate / 100);

// Optimal Taxation Point
optimal_tax = 50; // When happiness >= 50%
// At 50% tax + 50% happiness:
// gold_per_hour = capacity * 0.5 * 0.5 = capacity * 0.25

// Maximum Gold with Theater Level 10:
// happiness = 100 - 50 + 20 = 70%
// gold_per_hour = capacity * 0.7 * 0.5 = capacity * 0.35 (40% better)
```

### Research Scaling

**Combat Research Bonuses (Multiplicative)**
```javascript
// Metallurgy (most important)
attack_multiplier = 1 + (metallurgy_level * 0.05); // +5% per level
defense_multiplier = 1 + (metallurgy_level * 0.05);
// Levels 11+: ±10% per level

// Medicine
health_multiplier = 1 + (medicine_level * 0.05); // +5% per level
// Levels 11+: ±10% per level

// Dragonry (Dragon speed only)
dragon_speed_multiplier = 1 + (dragonry_level * 0.05); // +5% per level

// Rationing (Food consumption reduction)
food_consumption_multiplier = 1 - (rationing_level * 0.05); // -5% per level
// Level 20 = 0% food consumption
```

**Resource Production Scaling**
```javascript
// Base formula for Agriculture, Woodcraft, Masonry, Alloys
production_multiplier = 1 + (research_level * 0.10); // +10% per level

// Levels 11+:
// Agriculture: ±25% per level
// Others: ±20% per level

// Total Production with Wilderness
total = base * production_multiplier * (1 + wilderness_bonus);
wilderness_bonus = wilderness_level * 0.05; // Max 50% at L10
```

### Soul Revival (Spectral Ruins)

**Soul Recovery Time**
```javascript
// Formula
revival_time = (troops * base_training_time * 0.15) /
               (dark_portals + ((total_upgrades - portal_count) / 10));

// Soul Recovery Percentage
// Warrior Revival (offensive): Up to 95%
// Guardian Revival (defensive): Up to 95%
```

### Title Progression

**Power Requirement Formula**
```javascript
// Exponential scaling
P(1) = 0;
P(n) = 10 * Math.pow(2, n - 1); // for n > 1

// Examples:
// Level 2: 20 power
// Level 10: 5,120 power
// Level 20: 5,242,880 power
// Level 30: 5,368,709,120 power
// Level 52: 11,258,999,068,674,560 power
```

---

## PROGRESSION SYSTEMS

### Queue & Worker Systems

**Training Infrastructure**
```json
{
  "minimumTrainingTime": 30,
  "simultaneousQueues": "unlimited",
  "speedFactors": ["Garrison level", "Garrison quantity"],
  "eliteRequirements": {
    "trainingCampLevel": 10,
    "location": "outpost",
    "specialItems": "varies by troop"
  }
}
```

**Building Queue**
```json
{
  "simultaneousBuildings": 1,
  "speedup": "Rubies (Chronos)",
  "level10Cap": "Requires Completion Grant to bypass",
  "level11Plus": "Requires Ancestral Seals (1 per level)"
}
```

**Research Queue**
```json
{
  "simultaneousResearch": 1,
  "scienceCenterRequired": true,
  "crossFacility": "Multiple researches possible in different cities/outposts"
}
```

### Time Gate Mechanics

**Building Time Progression**
- Scaled by Levitation research
- Higher levels unlock via prerequisites
- Completion Grants/Ancestral Seals for late tiers

**Research Duration Scaling**
- Early (1-5): Minutes to hours
- Mid (6-10): Hours to days (2-3 days typical)
- Late (11-15): 5-7 days
- Very Late (16-20): 7+ days
- **Total game progression**: ~312 days for all researches

**Cooldown Systems**
```json
{
  "wavingBan": "1 hour (if rate-limited)",
  "tradeDuration": "30 minutes (fixed)",
  "wildernessRespawn": "15 minutes (initial)",
  "wildernessRegeneration": "10% per 5 minutes (50 min full)",
  "crimsonCitadelRespawn": "3 minutes"
}
```

### Progression Gates

**Dragon Access Timeline**
```json
{
  "immediate": ["Great Dragon"],
  "dragonry2": ["Swift Strike Dragons"],
  "dragonry3": ["Battle Dragons"],
  "wildernessL5Plus": ["Elemental dragon eggs (Water, Stone, Fire, Frost, Wind, Amber, Helio, Chrono)"],
  "dragonry1Plus": ["Sanctuary (breeding)"],
  "100kTalismans": ["Wraith Dragon", "Spectral Ruins unlock"]
}
```

**Elite Troops Gates**
```json
{
  "requirements": [
    "Training Camp Level 10 (outpost)",
    "Specific elite item (varies)",
    "Science/Research prerequisites (varies)"
  ]
}
```

**Advanced Features**
```json
{
  "spectralRuins": {
    "requirements": ["100K Anthropus Talismans", "Player Level 5+", "Forest Level 7+"]
  },
  "iceOutpost": {
    "requirements": ["Spectral Ruins first"]
  },
  "solarianHighlands": {
    "requirements": ["Spectral Ruins first"]
  },
  "sanctuary": {
    "requirements": ["Dragonry Level 1+"]
  }
}
```

### Early Game Optimization (First 24 Hours)

**Priority 1: Resource Production**
```json
{
  "fortress": 2,
  "farms": 1,
  "lumbermills": 3,
  "quarries": 3,
  "mines": 3,
  "homes": "5-7 for population"
}
```

**Priority 2: Military Infrastructure**
```json
{
  "garrisons": "~14 total",
  "sentinel": 1,
  "officersQuarters": 1,
  "musterPoint": 1,
  "storageVault": 1,
  "scienceCenter": 1,
  "metalsmith": 1,
  "factory": 1,
  "rookery": 1,
  "theater": "optional (for quests)"
}
```

**Priority 3: Research Path**
```json
{
  "immediate": ["Woodcraft to Level 4"],
  "early": ["Weapons Calibration (for Longbowmen)"],
  "beforePvP": ["Medicine", "Metallurgy"],
  "target": "Attack Level 5 Anthropus camps for Great Dragon Armor"
}
```

### SSD (Swift Strike Dragon) Build Strategy

**Aggressive Early Build**
```json
{
  "garrison": "1 at Level 5, rest at Level 1",
  "scienceCenter": 4,
  "rookery": 2,
  "resourceFields": "3 of each type",
  "production": "200 SSDs/hour",
  "research": {
    "dragonry": 6,
    "metallurgy": 5,
    "medicine": 4
  },
  "target": "Level 5+ Anthropus camps",
  "goal": "Rapid armor farming"
}
```

---

## IMPLEMENTATION NOTES

### Critical Balancing Metrics

**Combat Balance**
- Ranged units: 80+ damage at range → 5 damage in melee
- Defense stat: Directly subtracts from damage per unit
- Speed: Determines turn order (attacker wins ties)
- Health threshold: ~50% total health before losses begin
- Meat shield effectiveness: Smaller Armored Transport groups > larger groups

**Economic Balance**
- Optimal taxation: 50% when happiness ≥ 50%
- Theater value: 2% happiness per level = 10-20% at max
- Food consumption: Scales linearly, can reach 100% reduction at Rationing L20
- Trade efficiency: Highly realm-dependent (30M-999M gold for 1M resources)

**Progression Pacing**
- Early game (first week): Building basics, researching Levels 1-5
- Mid game (weeks 2-8): Research Levels 6-10, dragon armor farming
- Late game (months 2-12): Research Levels 11-20, elite troops, advanced features
- End game (year+): Title progression, alliance warfare, full optimization

**Server Performance Considerations**

1. **March Calculation**: Distance-based with speed modifiers
   - Cache common distances
   - Pre-calculate speed multipliers per research level

2. **Combat Resolution**: Turn-based deterministic
   - Sort troops by speed once per battle
   - Process groups (not individuals) for efficiency
   - Cache damage calculations per troop type combination

3. **Resource Production**: Time-based accumulation
   - Calculate on login (lazy)
   - Apply caps before returning
   - Store last update timestamp

4. **Wilderness Regeneration**: Percentage-based over time
   - 10% per 5 minutes = simple calculation
   - No need for constant polling
   - Calculate on access

5. **Alliance Ranking**: Sum-based power
   - Update on member power change
   - Cache alliance totals
   - Re-sort leaderboards periodically (not real-time)

### Anti-Cheat Validation Rules

**Resource Validation**
```javascript
// Server must verify:
- resources >= cost (at time of action, not intent)
- production_rate matches server calculation
- resource caps not exceeded
- wilderness bonuses valid (owns wilderness)
- research bonuses match server records
```

**Combat Validation**
```javascript
// Server must verify:
- troop counts match server records
- troops not currently on march
- target coordinates valid
- march distance calculated server-side
- combat outcome deterministic (same inputs = same outputs)
```

**Time Validation**
```javascript
// Server must verify:
- completion time = start time + duration (server-calculated)
- no speedup without ruby expenditure
- research/building prerequisites met
- queue limits not exceeded
```

**Economy Validation**
```javascript
// Server must verify:
- trade prices match realm market (not client-supplied)
- gold generation matches population * happiness * tax formula
- taxation changes logged (for rapid toggling detection)
- resource transfers have valid targets (alliance members)
```

---

## SOURCES

- [Dragons Of Atlantis Wiki - Buildings](https://dragonsofatlantis.fandom.com/wiki/Buildings)
- [Dragons Of Atlantis Wiki - Battle Mechanics](https://dragonsofatlantis.fandom.com/wiki/Battle_Mechanics)
- [Dragons Of Atlantis Wiki - Category:Troops](https://dragonsofatlantis.fandom.com/wiki/Category:Troops)
- [Dragons Of Atlantis Wiki - Category:Research](https://dragonsofatlantis.fandom.com/wiki/Category:Research)
- [DoA Tools - Battle Mechanics 101](https://doatools.wordpress.com/mechanics/battle-mech-101/)
- [DoA Wiki](https://doawiki.wordpress.com/)
- [Atlas of Atlantis](https://atlasofatlantis.com/)

---

*This document provides the complete mechanical foundation for implementing a server-authoritative Dragons of Atlantis game system with accurate progression, combat resolution, resource economics, and dragon mechanics.*
