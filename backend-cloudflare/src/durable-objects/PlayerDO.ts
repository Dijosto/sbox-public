/**
 * Player Durable Object - Enhanced Implementation
 * Manages all player state with queue systems, timers, and alarms
 */

import { Env } from '../workers/index';
import { getBuildingConfig, getBuildingLevelConfig, calculateBuildTime } from '../utils/buildings';
import { getTroopConfig, calculateTrainingTime } from '../utils/troops';
import { getResearchConfig, getResearchLevelConfig, checkResearchPrerequisites } from '../utils/research';
import {
  calculateProductionRates,
  calculateGoldProduction,
  calculateStorageCaps,
  calculateHomeCapacity,
  calculateAccumulatedResources,
  hasEnoughResources,
  deductResources,
  Resources
} from '../utils/resources';

interface PlayerState {
  playerId: string;
  playerName: string;
  city: CityState;
  resources: Resources;
  research: Record<string, number>;
  troops: TroopStack[];
  activeMarches: March[];
  taxRate: number;
  wilderness: Record<string, number>; // Conquered wilderness by type
  lastUpdateTimestamp: number;
}

interface CityState {
  position: { x: number; y: number };
  innerCity: Record<string, BuildingState>;
  outerFields: Record<string, BuildingState>;
  buildQueue: BuildQueueItem[];
  trainQueue: TrainQueueItem[];
  researchQueue: ResearchQueueItem[];
  maxWorkers: number;
}

interface BuildingState {
  buildingType: string;
  level: number;
}

interface BuildQueueItem {
  queueId: string;
  buildingId: string; // Unique building instance ID (e.g., "farm_1")
  buildingType: string;
  zone: string;
  toLevel: number;
  startTime: number;
  completionTime: number;
}

interface TrainQueueItem {
  queueId: string;
  troopType: string;
  quantity: number;
  startTime: number;
  completionTime: number;
}

interface ResearchQueueItem {
  queueId: string;
  researchType: string;
  toLevel: number;
  startTime: number;
  completionTime: number;
}

interface TroopStack {
  troopType: string;
  quantity: number;
  location: string;
}

interface March {
  marchId: string;
  origin: { x: number; y: number };
  destination: { x: number; y: number };
  army: TroopStack[];
  startTime: number;
  arrivalTime: number;
  marchType: string;
}

export class PlayerDurableObject {
  private state: DurableObjectState;
  private env: Env;
  private playerState: PlayerState | null = null;
  private websocket: WebSocket | null = null;

  constructor(state: DurableObjectState, env: Env) {
    this.state = state;
    this.env = env;
  }

  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    // Handle WebSocket upgrade
    if (request.headers.get('Upgrade') === 'websocket') {
      return this.handleWebSocket(request);
    }

    // Load state if not loaded
    if (!this.playerState) {
      await this.loadState();
    }

    // Route requests
    switch (url.pathname) {
      case '/initialize':
        return this.handleInitialize(request);

      case '/api/player/state':
        return this.handleGetState(request);

      // Building endpoints
      case '/api/player/building/upgrade':
        return this.handleBuildingUpgrade(request);

      case '/api/player/building/cancel':
        return this.handleBuildingCancel(request);

      // Training endpoints
      case '/api/player/troops/train':
        return this.handleTrainTroops(request);

      case '/api/player/troops/cancel':
        return this.handleTrainCancel(request);

      // Research endpoints
      case '/api/player/research/start':
        return this.handleStartResearch(request);

      case '/api/player/research/cancel':
        return this.handleResearchCancel(request);

      // Internal completion handlers (called by alarms)
      case '/internal/complete':
        return this.handleCompletions(request);

      default:
        return new Response('Not Found', { status: 404 });
    }
  }

  /**
   * Durable Object alarm handler
   * Called automatically when alarm fires
   */
  async alarm(): Promise<void> {
    console.log('[PlayerDO] Alarm fired, processing completions');

    if (!this.playerState) {
      await this.loadState();
    }

    if (!this.playerState) {
      console.error('[PlayerDO] No state loaded on alarm');
      return;
    }

    const now = Date.now();
    let hasCompletions = false;

    // Process completed buildings
    const completedBuilds = this.playerState.city.buildQueue.filter(
      item => item.completionTime <= now
    );

    for (const build of completedBuilds) {
      await this.completeBuild(build);
      hasCompletions = true;
    }

    // Process completed training
    const completedTraining = this.playerState.city.trainQueue.filter(
      item => item.completionTime <= now
    );

    for (const train of completedTraining) {
      await this.completeTraining(train);
      hasCompletions = true;
    }

    // Process completed research
    const completedResearch = this.playerState.city.researchQueue.filter(
      item => item.completionTime <= now
    );

    for (const research of completedResearch) {
      await this.completeResearch(research);
      hasCompletions = true;
    }

    if (hasCompletions) {
      await this.saveState();
    }

    // Schedule next alarm
    await this.scheduleNextAlarm();
  }

  /**
   * Load player state from storage
   */
  private async loadState(): Promise<void> {
    const stored = await this.state.storage.get<PlayerState>('state');

    if (stored) {
      this.playerState = stored;
      this.updateResources();
    }
  }

  /**
   * Save player state to storage
   */
  private async saveState(): Promise<void> {
    if (this.playerState) {
      this.playerState.lastUpdateTimestamp = Date.now();
      await this.state.storage.put('state', this.playerState);
    }
  }

  /**
   * Update resources based on production rates
   */
  private updateResources(): void {
    if (!this.playerState) return;

    const now = Date.now();
    const elapsed = (now - this.playerState.lastUpdateTimestamp) / (1000 * 60 * 60); // hours

    // Calculate production rates
    const allBuildings = {
      ...this.playerState.city.innerCity,
      ...this.playerState.city.outerFields
    };

    const rates = calculateProductionRates(
      allBuildings,
      this.playerState.research,
      this.playerState.wilderness
    );

    // Add gold from taxation
    const homeCapacity = calculateHomeCapacity(allBuildings);
    const theaterLevel = allBuildings['theater']?.level || 0;
    rates.goldRate = calculateGoldProduction(homeCapacity, this.playerState.taxRate, theaterLevel);

    // Calculate storage caps
    const caps = calculateStorageCaps(allBuildings);

    // Update resources
    this.playerState.resources = calculateAccumulatedResources(
      this.playerState.resources,
      rates,
      caps,
      elapsed
    );

    // Update rates and caps in state
    this.playerState.resources.foodRate = rates.foodRate;
    this.playerState.resources.woodRate = rates.woodRate;
    this.playerState.resources.stoneRate = rates.stoneRate;
    this.playerState.resources.metalRate = rates.metalRate;
    this.playerState.resources.goldRate = rates.goldRate;

    this.playerState.resources.foodCap = caps.foodCap;
    this.playerState.resources.woodCap = caps.woodCap;
    this.playerState.resources.stoneCap = caps.stoneCap;
    this.playerState.resources.metalCap = caps.metalCap;
    this.playerState.resources.goldCap = caps.goldCap;
  }

  /**
   * Initialize new player
   */
  private async handleInitialize(request: Request): Promise<Response> {
    const body = await request.json() as any;

    this.playerState = {
      playerId: body.playerId,
      playerName: body.playerName,
      city: {
        position: { x: 500, y: 500 },
        innerCity: {
          fortress: { buildingType: 'fortress', level: 1 },
          home_1: { buildingType: 'home', level: 1 },
        },
        outerFields: {
          farm_1: { buildingType: 'farm', level: 1 },
          lumbermill_1: { buildingType: 'lumbermill', level: 1 },
          quarry_1: { buildingType: 'quarry', level: 1 },
          mine_1: { buildingType: 'mine', level: 1 },
        },
        buildQueue: [],
        trainQueue: [],
        researchQueue: [],
        maxWorkers: 1,
      },
      resources: {
        food: 1000,
        wood: 1000,
        stone: 500,
        metal: 200,
        gold: 100,
        premiumCurrency: 0,
        foodRate: 100,
        woodRate: 100,
        stoneRate: 50,
        metalRate: 25,
        goldRate: 10,
        foodCap: 10000,
        woodCap: 10000,
        stoneCap: 5000,
        metalCap: 2500,
        goldCap: 1000,
      },
      research: {},
      troops: [],
      activeMarches: [],
      taxRate: 25, // Default 25% tax
      wilderness: {},
      lastUpdateTimestamp: Date.now(),
    };

    await this.saveState();

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Get player state
   */
  private async handleGetState(request: Request): Promise<Response> {
    if (!this.playerState) {
      return new Response(JSON.stringify({ error: 'State not loaded' }), {
        status: 500,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    this.updateResources();
    await this.saveState();

    return new Response(JSON.stringify(this.playerState), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Handle building upgrade request
   */
  private async handleBuildingUpgrade(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      buildingId: string;
      buildingType: string;
      zone: 'inner' | 'outer';
    };

    // Update resources
    this.updateResources();

    // Get building config
    const buildingConfig = getBuildingConfig(body.buildingType);
    if (!buildingConfig) {
      return this.errorResponse(`Unknown building type: ${body.buildingType}`);
    }

    // Check zone
    const buildings = body.zone === 'inner'
      ? this.playerState.city.innerCity
      : this.playerState.city.outerFields;

    // Find existing building by ID or by type
    let currentBuilding = buildings[body.buildingId];

    // If not found by ID, search by type (for unique buildings)
    if (!currentBuilding) {
      const existingKey = Object.keys(buildings).find(
        key => buildings[key].buildingType === body.buildingType
      );
      if (existingKey) {
        currentBuilding = buildings[existingKey];
      }
    }

    const currentLevel = currentBuilding?.level || 0;
    const targetLevel = currentLevel + 1;

    // Check if building can be upgraded
    if (targetLevel > buildingConfig.maxLevel) {
      return this.errorResponse(`Building already at max level (${buildingConfig.maxLevel})`);
    }

    // Get level config
    const levelConfig = getBuildingLevelConfig(body.buildingType, targetLevel);
    if (!levelConfig) {
      return this.errorResponse(`Level ${targetLevel} not found for ${body.buildingType}`);
    }

    // Check prerequisites
    for (const prereq of levelConfig.prerequisites) {
      if (prereq.type === 'building') {
        const prereqBuilding = this.playerState.city.innerCity[prereq.id] ||
                               this.playerState.city.outerFields[prereq.id];
        if (!prereqBuilding || prereqBuilding.level < prereq.level) {
          return this.errorResponse(`Requires ${prereq.id} level ${prereq.level}`);
        }
      } else if (prereq.type === 'research') {
        const researchLevel = this.playerState.research[prereq.id] || 0;
        if (researchLevel < prereq.level) {
          return this.errorResponse(`Requires ${prereq.id} research level ${prereq.level}`);
        }
      }
    }

    // Check worker availability
    const usedWorkers = this.playerState.city.buildQueue.length;
    if (usedWorkers >= this.playerState.city.maxWorkers) {
      return this.errorResponse(`All workers busy (${usedWorkers}/${this.playerState.city.maxWorkers})`);
    }

    // Check resources
    const resourceCheck = hasEnoughResources(this.playerState.resources, levelConfig.cost);
    if (!resourceCheck.valid) {
      return this.errorResponse(`Insufficient ${resourceCheck.missing}`);
    }

    // Deduct resources
    this.playerState.resources = deductResources(this.playerState.resources, levelConfig.cost);

    // Calculate build time with Levitation research
    const levitationLevel = this.playerState.research['levitation'] || 0;
    const buildTime = calculateBuildTime(levelConfig.buildTime, levitationLevel);

    // Add to queue
    const now = Date.now();
    const queueItem: BuildQueueItem = {
      queueId: crypto.randomUUID(),
      buildingId: body.buildingId,
      buildingType: body.buildingType,
      zone: body.zone,
      toLevel: targetLevel,
      startTime: now,
      completionTime: now + (buildTime * 1000)
    };

    this.playerState.city.buildQueue.push(queueItem);

    // Save and schedule alarm
    await this.saveState();
    await this.scheduleNextAlarm();

    return new Response(JSON.stringify({
      success: true,
      queueId: queueItem.queueId,
      completionTime: queueItem.completionTime,
      duration: buildTime
    }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Cancel building upgrade
   */
  private async handleBuildingCancel(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as { queueId: string };

    const index = this.playerState.city.buildQueue.findIndex(item => item.queueId === body.queueId);
    if (index === -1) {
      return this.errorResponse('Queue item not found');
    }

    const item = this.playerState.city.buildQueue[index];

    // Get refund amount (50% of cost)
    const levelConfig = getBuildingLevelConfig(item.buildingType, item.toLevel);
    if (levelConfig) {
      this.playerState.resources.food += Math.floor((levelConfig.cost.food || 0) * 0.5);
      this.playerState.resources.wood += Math.floor((levelConfig.cost.wood || 0) * 0.5);
      this.playerState.resources.stone += Math.floor((levelConfig.cost.stone || 0) * 0.5);
      this.playerState.resources.metal += Math.floor((levelConfig.cost.metal || 0) * 0.5);
      this.playerState.resources.gold += Math.floor((levelConfig.cost.gold || 0) * 0.5);
    }

    // Remove from queue
    this.playerState.city.buildQueue.splice(index, 1);

    await this.saveState();
    await this.scheduleNextAlarm();

    this.pushEvent('building_cancelled', { queueId: body.queueId });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Complete building upgrade
   */
  private async completeBuild(item: BuildQueueItem): Promise<void> {
    if (!this.playerState) return;

    const buildings = item.zone === 'inner'
      ? this.playerState.city.innerCity
      : this.playerState.city.outerFields;

    // Find existing building by ID or by type (for unique buildings like fortress)
    let actualBuildingKey = item.buildingId;

    // If buildingId doesn't exist, search for existing building of same type
    if (!buildings[item.buildingId]) {
      // For unique buildings (fortress, science center, etc), find by type
      const existingKey = Object.keys(buildings).find(
        key => buildings[key].buildingType === item.buildingType
      );

      if (existingKey) {
        actualBuildingKey = existingKey;
      }
    }

    // Create or upgrade building
    if (!buildings[actualBuildingKey]) {
      buildings[actualBuildingKey] = {
        buildingType: item.buildingType,
        level: item.toLevel
      };
    } else {
      buildings[actualBuildingKey].level = item.toLevel;
    }

    // Remove from queue
    this.playerState.city.buildQueue = this.playerState.city.buildQueue.filter(
      q => q.queueId !== item.queueId
    );

    // Update max workers if fortress upgraded
    if (item.buildingType === 'fortress') {
      this.playerState.city.maxWorkers = Math.floor(item.toLevel / 5) + 1;
    }

    // Recalculate production rates
    this.updateResources();

    // Notify client
    this.pushEvent('building_complete', {
      buildingId: actualBuildingKey,
      buildingType: item.buildingType,
      level: item.toLevel
    });

    console.log(`[PlayerDO] Completed build: ${item.buildingType} to level ${item.toLevel}`);
  }

  /**
   * Schedule next alarm based on pending queues
   */
  private async scheduleNextAlarm(): Promise<void> {
    if (!this.playerState) return;

    const now = Date.now();
    let nextCompletion: number | null = null;

    // Find earliest completion time across all queues
    for (const item of this.playerState.city.buildQueue) {
      if (item.completionTime > now) {
        if (!nextCompletion || item.completionTime < nextCompletion) {
          nextCompletion = item.completionTime;
        }
      }
    }

    for (const item of this.playerState.city.trainQueue) {
      if (item.completionTime > now) {
        if (!nextCompletion || item.completionTime < nextCompletion) {
          nextCompletion = item.completionTime;
        }
      }
    }

    for (const item of this.playerState.city.researchQueue) {
      if (item.completionTime > now) {
        if (!nextCompletion || item.completionTime < nextCompletion) {
          nextCompletion = item.completionTime;
        }
      }
    }

    if (nextCompletion) {
      await this.state.storage.setAlarm(nextCompletion);
      console.log(`[PlayerDO] Scheduled alarm for ${new Date(nextCompletion).toISOString()}`);
    }
  }

  /**
   * Handle internal completion processing
   */
  private async handleCompletions(request: Request): Promise<Response> {
    // Manually trigger completion check (for testing)
    await this.alarm();
    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Handle troop training request
   */
  private async handleTrainTroops(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      troopType: string;
      quantity: number;
    };

    // Update resources
    this.updateResources();

    // Get troop config
    const troopConfig = getTroopConfig(body.troopType);
    if (!troopConfig) {
      return this.errorResponse(`Unknown troop type: ${body.troopType}`);
    }

    // Check prerequisites
    for (const prereq of troopConfig.prerequisites) {
      if (prereq.type === 'building') {
        const building = this.playerState.city.innerCity[prereq.id] ||
                        this.playerState.city.outerFields[prereq.id];
        if (!building || building.level < prereq.level) {
          return this.errorResponse(`Requires ${prereq.id} level ${prereq.level}`);
        }
      } else if (prereq.type === 'research') {
        const researchLevel = this.playerState.research[prereq.id] || 0;
        if (researchLevel < prereq.level) {
          return this.errorResponse(`Requires ${prereq.id} research level ${prereq.level}`);
        }
      }
    }

    // Calculate total cost
    const totalCost = {
      food: (troopConfig.cost.food || 0) * body.quantity,
      wood: (troopConfig.cost.wood || 0) * body.quantity,
      stone: (troopConfig.cost.stone || 0) * body.quantity,
      metal: (troopConfig.cost.metal || 0) * body.quantity
    };

    // Check resources
    const resourceCheck = hasEnoughResources(this.playerState.resources, totalCost);
    if (!resourceCheck.valid) {
      return this.errorResponse(`Insufficient ${resourceCheck.missing}`);
    }

    // Deduct resources
    this.playerState.resources = deductResources(this.playerState.resources, totalCost);

    // Calculate training time
    const garrisonLevels: number[] = [];
    let garrisonCount = 0;

    for (const [id, building] of Object.entries(this.playerState.city.innerCity)) {
      if (building.buildingType === 'garrison') {
        garrisonLevels.push(building.level);
        garrisonCount++;
      }
    }

    const trainingTime = calculateTrainingTime(
      troopConfig.trainingTime,
      body.quantity,
      garrisonLevels,
      garrisonCount
    );

    // Add to queue
    const now = Date.now();
    const queueItem: TrainQueueItem = {
      queueId: crypto.randomUUID(),
      troopType: body.troopType,
      quantity: body.quantity,
      startTime: now,
      completionTime: now + (trainingTime * 1000)
    };

    this.playerState.city.trainQueue.push(queueItem);

    // Save and schedule alarm
    await this.saveState();
    await this.scheduleNextAlarm();

    return new Response(JSON.stringify({
      success: true,
      queueId: queueItem.queueId,
      completionTime: queueItem.completionTime,
      duration: trainingTime
    }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Cancel troop training
   */
  private async handleTrainCancel(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as { queueId: string };

    const index = this.playerState.city.trainQueue.findIndex(item => item.queueId === body.queueId);
    if (index === -1) {
      return this.errorResponse('Queue item not found');
    }

    const item = this.playerState.city.trainQueue[index];

    // Get refund amount (50% of cost)
    const troopConfig = getTroopConfig(item.troopType);
    if (troopConfig) {
      const refundMultiplier = 0.5;
      this.playerState.resources.food += Math.floor((troopConfig.cost.food || 0) * item.quantity * refundMultiplier);
      this.playerState.resources.wood += Math.floor((troopConfig.cost.wood || 0) * item.quantity * refundMultiplier);
      this.playerState.resources.stone += Math.floor((troopConfig.cost.stone || 0) * item.quantity * refundMultiplier);
      this.playerState.resources.metal += Math.floor((troopConfig.cost.metal || 0) * item.quantity * refundMultiplier);
    }

    // Remove from queue
    this.playerState.city.trainQueue.splice(index, 1);

    await this.saveState();
    await this.scheduleNextAlarm();

    this.pushEvent('training_cancelled', { queueId: body.queueId });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Complete troop training
   */
  private async completeTraining(item: TrainQueueItem): Promise<void> {
    if (!this.playerState) return;

    // Add troops to player's army
    const existingStack = this.playerState.troops.find(
      t => t.troopType === item.troopType && t.location === 'city'
    );

    if (existingStack) {
      existingStack.quantity += item.quantity;
    } else {
      this.playerState.troops.push({
        troopType: item.troopType,
        quantity: item.quantity,
        location: 'city'
      });
    }

    // Remove from queue
    this.playerState.city.trainQueue = this.playerState.city.trainQueue.filter(
      q => q.queueId !== item.queueId
    );

    // Notify client
    this.pushEvent('training_complete', {
      troopType: item.troopType,
      quantity: item.quantity
    });

    console.log(`[PlayerDO] Completed training: ${item.quantity}x ${item.troopType}`);
  }

  /**
   * Handle research start request
   */
  private async handleStartResearch(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      researchType: string;
    };

    // Update resources
    this.updateResources();

    // Get research config
    const researchConfig = getResearchConfig(body.researchType);
    if (!researchConfig) {
      return this.errorResponse(`Unknown research type: ${body.researchType}`);
    }

    // Check if already researching
    if (this.playerState.city.researchQueue.length > 0) {
      return this.errorResponse('Already researching');
    }

    // Get current level
    const currentLevel = this.playerState.research[body.researchType] || 0;
    const targetLevel = currentLevel + 1;

    // Check max level
    if (targetLevel > researchConfig.maxLevel) {
      return this.errorResponse(`Research already at max level (${researchConfig.maxLevel})`);
    }

    // Get level config
    const levelConfig = getResearchLevelConfig(body.researchType, targetLevel);
    if (!levelConfig) {
      return this.errorResponse(`Level ${targetLevel} not found for ${body.researchType}`);
    }

    // Check prerequisites
    const prereqCheck = checkResearchPrerequisites(levelConfig, this.playerState);
    if (!prereqCheck.valid) {
      return this.errorResponse(prereqCheck.reason || 'Prerequisites not met');
    }

    // Check resources
    const resourceCheck = hasEnoughResources(this.playerState.resources, levelConfig.cost);
    if (!resourceCheck.valid) {
      return this.errorResponse(`Insufficient ${resourceCheck.missing}`);
    }

    // Deduct resources
    this.playerState.resources = deductResources(this.playerState.resources, levelConfig.cost);

    // Add to queue
    const now = Date.now();
    const queueItem: ResearchQueueItem = {
      queueId: crypto.randomUUID(),
      researchType: body.researchType,
      toLevel: targetLevel,
      startTime: now,
      completionTime: now + (levelConfig.researchTime * 1000)
    };

    this.playerState.city.researchQueue.push(queueItem);

    // Save and schedule alarm
    await this.saveState();
    await this.scheduleNextAlarm();

    return new Response(JSON.stringify({
      success: true,
      queueId: queueItem.queueId,
      completionTime: queueItem.completionTime,
      duration: levelConfig.researchTime
    }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Cancel research
   */
  private async handleResearchCancel(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as { queueId: string };

    const index = this.playerState.city.researchQueue.findIndex(item => item.queueId === body.queueId);
    if (index === -1) {
      return this.errorResponse('Queue item not found');
    }

    const item = this.playerState.city.researchQueue[index];

    // Get refund amount (50% of cost)
    const levelConfig = getResearchLevelConfig(item.researchType, item.toLevel);
    if (levelConfig) {
      this.playerState.resources.food += Math.floor((levelConfig.cost.food || 0) * 0.5);
      this.playerState.resources.wood += Math.floor((levelConfig.cost.wood || 0) * 0.5);
      this.playerState.resources.stone += Math.floor((levelConfig.cost.stone || 0) * 0.5);
      this.playerState.resources.metal += Math.floor((levelConfig.cost.metal || 0) * 0.5);
      this.playerState.resources.gold += Math.floor((levelConfig.cost.gold || 0) * 0.5);
    }

    // Remove from queue
    this.playerState.city.researchQueue.splice(index, 1);

    await this.saveState();
    await this.scheduleNextAlarm();

    this.pushEvent('research_cancelled', { queueId: body.queueId });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Complete research
   */
  private async completeResearch(item: ResearchQueueItem): Promise<void> {
    if (!this.playerState) return;

    // Update research level
    this.playerState.research[item.researchType] = item.toLevel;

    // Remove from queue
    this.playerState.city.researchQueue = this.playerState.city.researchQueue.filter(
      q => q.queueId !== item.queueId
    );

    // Recalculate production rates (research may affect them)
    this.updateResources();

    // Notify client
    this.pushEvent('research_complete', {
      researchType: item.researchType,
      level: item.toLevel
    });

    console.log(`[PlayerDO] Completed research: ${item.researchType} to level ${item.toLevel}`);
  }

  /**
   * WebSocket handling
   */
  private handleWebSocket(request: Request): Response {
    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);

    this.state.acceptWebSocket(server);
    this.websocket = server;

    server.send(JSON.stringify({
      type: 'connection',
      timestamp: Date.now(),
      data: JSON.stringify({ status: 'connected' }),
    }));

    return new Response(null, {
      status: 101,
      webSocket: client,
    });
  }

  async webSocketMessage(ws: WebSocket, message: string | ArrayBuffer): Promise<void> {
    console.log('[PlayerDO] WebSocket message:', message);
  }

  async webSocketClose(ws: WebSocket, code: number, reason: string, wasClean: boolean): Promise<void> {
    this.websocket = null;
    console.log('[PlayerDO] WebSocket closed:', code, reason);
  }

  private pushEvent(eventType: string, data: any): void {
    if (this.websocket && this.websocket.readyState === 1) {
      this.websocket.send(JSON.stringify({
        type: eventType,
        timestamp: Date.now(),
        data: JSON.stringify(data),
      }));
    }
  }

  /**
   * Helper for error responses
   */
  private errorResponse(error: string, status: number = 400): Response {
    return new Response(JSON.stringify({ success: false, error }), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  }
}
