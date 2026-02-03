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
import {
  calculateDistance,
  calculateMarchTime,
  calculateMarchCapacity,
  validateMarch,
  MarchTroops
} from '../utils/marches';
import { resolveCombat, calculateLoot, CombatSide, CombatTroop } from '../utils/combat';

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
  targetId?: string;
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

      case '/api/player/buildings':
        return this.handleGetBuildings(request);

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

      case '/api/player/troops/station':
        return this.handleStationTroops(request);

      case '/api/player/troops/unstation':
        return this.handleUnstationTroops(request);

      // Research endpoints
      case '/api/player/research/start':
        return this.handleStartResearch(request);

      case '/api/player/research/cancel':
        return this.handleResearchCancel(request);

      // March endpoints
      case '/api/player/march/send':
        return this.handleSendMarch(request);

      case '/api/player/march/recall':
        return this.handleRecallMarch(request);

      // Message endpoints
      case '/api/player/messages':
        return this.handleGetMessages(request);

      case '/api/player/messages/read':
        return this.handleMarkMessageRead(request);

      // Internal completion handlers (called by alarms)
      case '/internal/complete':
        return this.handleCompletions(request);

      case '/internal/plunder':
        return this.handlePlunder(request);

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

    // Process march arrivals
    const arrivedMarches = this.playerState.activeMarches.filter(
      march => march.status === 'outbound' && march.arrivalTime <= now
    );

    for (const march of arrivedMarches) {
      await this.processMarchArrival(march);
      hasCompletions = true;
    }

    // Process march returns
    const returningMarches = this.playerState.activeMarches.filter(
      march => march.status === 'returning' && march.returnTime && march.returnTime <= now
    );

    for (const march of returningMarches) {
      await this.processMarchReturn(march);
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

    // Update production rates in state
    this.playerState.resources.foodRate = rates.foodRate;
    this.playerState.resources.woodRate = rates.woodRate;
    this.playerState.resources.stoneRate = rates.stoneRate;
    this.playerState.resources.metalRate = rates.metalRate;
    this.playerState.resources.goldRate = rates.goldRate;
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
        position: { x: body.cityX || 500, y: body.cityY || 500 }, // Use spawn coordinates from auth
        innerCity: {
          fortress_1: { buildingType: 'fortress', level: 1 },
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
        food: 10000,
        wood: 10000,
        stone: 10000,
        metal: 10000,
        gold: 1000,
        premiumCurrency: 0,
        foodRate: 100,
        woodRate: 100,
        stoneRate: 50,
        metalRate: 25,
        goldRate: 10,
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
   * Get all buildings with their exact IDs for client UI
   */
  private async handleGetBuildings(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    this.updateResources();

    // Return all building slots with their IDs, types, and levels
    const buildingsList = {
      innerCity: Object.entries(this.playerState.city.innerCity).map(([id, building]) => ({
        id,
        buildingType: building.buildingType,
        level: building.level,
        zone: 'inner' as const
      })),
      outerFields: Object.entries(this.playerState.city.outerFields).map(([id, building]) => ({
        id,
        buildingType: building.buildingType,
        level: building.level,
        zone: 'outer' as const
      }))
    };

    return new Response(JSON.stringify(buildingsList), {
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

    // Check marches (arrivals and returns)
    for (const march of this.playerState.activeMarches) {
      if (march.status === 'outbound' && march.arrivalTime > now) {
        if (!nextCompletion || march.arrivalTime < nextCompletion) {
          nextCompletion = march.arrivalTime;
        }
      }
      if (march.status === 'returning' && march.returnTime && march.returnTime > now) {
        if (!nextCompletion || march.returnTime < nextCompletion) {
          nextCompletion = march.returnTime;
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

    // Calculate training time - requires at least one garrison
    const garrisonLevels: number[] = [];
    let garrisonCount = 0;

    for (const [id, building] of Object.entries(this.playerState.city.innerCity)) {
      if (building.buildingType === 'garrison') {
        garrisonLevels.push(building.level);
        garrisonCount++;
      }
    }

    // Cannot train troops without a garrison
    if (garrisonCount === 0) {
      return this.errorResponse('You must build a Garrison to train troops');
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
   * Station troops on the wall for defense
   */
  private async handleStationTroops(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      troopType: string;
      quantity: number;
    };

    // Update resources
    this.updateResources();

    // Find troops in city
    const cityStack = this.playerState.troops.find(
      t => t.troopType === body.troopType && t.location === 'city'
    );

    if (!cityStack || cityStack.quantity < body.quantity) {
      return this.errorResponse(`Insufficient ${body.troopType} in city (need ${body.quantity}, have ${cityStack?.quantity || 0})`);
    }

    // Remove from city
    cityStack.quantity -= body.quantity;
    if (cityStack.quantity === 0) {
      this.playerState.troops = this.playerState.troops.filter(t => t !== cityStack);
    }

    // Add to wall
    const wallStack = this.playerState.troops.find(
      t => t.troopType === body.troopType && t.location === 'wall'
    );

    if (wallStack) {
      wallStack.quantity += body.quantity;
    } else {
      this.playerState.troops.push({
        troopType: body.troopType,
        quantity: body.quantity,
        location: 'wall'
      });
    }

    await this.saveState();

    this.pushEvent('troops_stationed', {
      troopType: body.troopType,
      quantity: body.quantity
    });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Unstation troops from the wall back to city
   */
  private async handleUnstationTroops(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      troopType: string;
      quantity: number;
    };

    // Update resources
    this.updateResources();

    // Find troops on wall
    const wallStack = this.playerState.troops.find(
      t => t.troopType === body.troopType && t.location === 'wall'
    );

    if (!wallStack || wallStack.quantity < body.quantity) {
      return this.errorResponse(`Insufficient ${body.troopType} on wall (need ${body.quantity}, have ${wallStack?.quantity || 0})`);
    }

    // Remove from wall
    wallStack.quantity -= body.quantity;
    if (wallStack.quantity === 0) {
      this.playerState.troops = this.playerState.troops.filter(t => t !== wallStack);
    }

    // Add to city
    const cityStack = this.playerState.troops.find(
      t => t.troopType === body.troopType && t.location === 'city'
    );

    if (cityStack) {
      cityStack.quantity += body.quantity;
    } else {
      this.playerState.troops.push({
        troopType: body.troopType,
        quantity: body.quantity,
        location: 'city'
      });
    }

    await this.saveState();

    this.pushEvent('troops_unstationed', {
      troopType: body.troopType,
      quantity: body.quantity
    });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
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
   * Send march (attack, gather, scout, etc.)
   */
  private async handleSendMarch(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      destination: { x: number; y: number };
      troops: MarchTroops[];
      marchType: 'attack' | 'gather' | 'scout' | 'reinforce' | 'transport';
      targetType: 'player' | 'npc' | 'wilderness';
      targetId?: string;
    };

    // Update resources
    this.updateResources();

    // Validate march
    const validation = validateMarch(
      body.troops,
      this.playerState.troops.map(t => ({ troopType: t.troopType, quantity: t.quantity })),
      this.playerState.city.position,
      body.destination
    );

    if (!validation.valid) {
      return this.errorResponse(validation.error || 'Invalid march');
    }

    // Calculate distance and travel time
    const distance = calculateDistance(this.playerState.city.position, body.destination);
    const marchSpeed = this.playerState.research['logistics'] || 0; // Speed research bonus
    const travelTime = calculateMarchTime(distance, body.troops, marchSpeed * 10);

    // Deduct troops from player
    for (const marchTroop of body.troops) {
      const playerTroop = this.playerState.troops.find(t => t.troopType === marchTroop.troopType);
      if (playerTroop) {
        playerTroop.quantity -= marchTroop.quantity;
        if (playerTroop.quantity <= 0) {
          this.playerState.troops = this.playerState.troops.filter(t => t.troopType !== marchTroop.troopType);
        }
      }
    }

    // Create march
    const now = Date.now();
    const march: March = {
      marchId: crypto.randomUUID(),
      playerId: this.playerState.playerId,
      playerName: this.playerState.playerName,
      origin: this.playerState.city.position,
      destination: body.destination,
      troops: body.troops,
      marchType: body.marchType,
      departureTime: now,
      arrivalTime: now + (travelTime * 1000),
      status: 'outbound',
      targetType: body.targetType,
      targetId: body.targetId
    };

    this.playerState.activeMarches.push(march);

    // Save and schedule alarm
    await this.saveState();
    await this.scheduleNextAlarm();

    this.pushEvent('march_sent', {
      marchId: march.marchId,
      arrivalTime: march.arrivalTime,
      duration: travelTime
    });

    return new Response(JSON.stringify({
      success: true,
      marchId: march.marchId,
      arrivalTime: march.arrivalTime,
      duration: travelTime,
      distance
    }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Recall a march (only works if outbound or at target)
   */
  private async handleRecallMarch(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as { marchId: string };

    const march = this.playerState.activeMarches.find(m => m.marchId === body.marchId);
    if (!march) {
      return this.errorResponse('March not found');
    }

    if (march.status === 'returning' || march.status === 'completed') {
      return this.errorResponse('March cannot be recalled');
    }

    const now = Date.now();

    // If march hasn't arrived yet, recall from current position
    if (march.status === 'outbound' && now < march.arrivalTime) {
      // Calculate current position (for simplicity, just reverse from destination)
      const marchSpeed = this.playerState.research['logistics'] || 0;
      const returnTime = calculateMarchTime(
        calculateDistance(march.destination, march.origin),
        march.troops,
        marchSpeed * 10
      );

      march.status = 'returning';
      march.returnTime = now + (returnTime * 1000);

      await this.saveState();
      await this.scheduleNextAlarm();

      this.pushEvent('march_recalled', { marchId: march.marchId });

      return new Response(JSON.stringify({
        success: true,
        returnTime: march.returnTime
      }), {
        headers: { 'Content-Type': 'application/json' },
      });
    }

    // If at target, start return journey
    if (march.status === 'at_target') {
      const marchSpeed = this.playerState.research['logistics'] || 0;
      const returnTime = calculateMarchTime(
        calculateDistance(march.destination, march.origin),
        march.troops,
        marchSpeed * 10
      );

      march.status = 'returning';
      march.returnTime = now + (returnTime * 1000);

      await this.saveState();
      await this.scheduleNextAlarm();

      this.pushEvent('march_returning', { marchId: march.marchId });

      return new Response(JSON.stringify({
        success: true,
        returnTime: march.returnTime
      }), {
        headers: { 'Content-Type': 'application/json' },
      });
    }

    return this.errorResponse('March cannot be recalled');
  }

  /**
   * Process march arrival at target
   */
  private async processMarchArrival(march: March): Promise<void> {
    if (!this.playerState) return;

    console.log(`[PlayerDO] March ${march.marchId} arrived at target`);

    // Handle different march types
    if (march.marchType === 'attack' && march.targetType === 'npc') {
      // Query NPC camp from database
      const npcCamp = await this.env.DB.prepare(
        'SELECT * FROM npc_camps WHERE x = ? AND y = ?'
      ).bind(march.destination.x, march.destination.y).first();

      if (!npcCamp) {
        console.error(`[PlayerDO] NPC camp not found at ${march.destination.x},${march.destination.y}`);
        march.status = 'returning';
        march.returnTime = Date.now() + 10000; // Return in 10s
        return;
      }

      // Parse NPC garrison and resources
      const npcGarrison = JSON.parse(npcCamp.garrison as string);
      const npcResources = JSON.parse(npcCamp.resources as string);

      // Convert march troops to combat format
      const attackerTroops: CombatTroop[] = march.troops.map(t => ({
        troopType: t.troopType,
        quantity: t.quantity
      }));

      // Convert NPC garrison to combat format
      const defenderTroops: CombatTroop[] = Object.entries(npcGarrison).map(([type, data]: [string, any]) => ({
        troopType: type,
        quantity: data.quantity
      }));

      // Prepare combat sides
      const attacker: CombatSide = {
        playerId: this.playerState.playerId,
        playerName: this.playerState.playerName,
        troops: attackerTroops,
        research: this.playerState.research,
        dragonBonus: 0 // TODO: Add dragon bonuses
      };

      const defender: CombatSide = {
        playerId: 'npc_' + npcCamp.camp_id,
        playerName: `${npcCamp.camp_type} Camp Lv.${npcCamp.level}`,
        troops: defenderTroops,
        research: {}, // NPCs don't have research
        dragonBonus: 0
      };

      // Resolve combat
      const combatResult = resolveCombat(attacker, defender);

      // Calculate loot if attacker won
      let loot = null;
      if (combatResult.winner === 'attacker') {
        const capacity = calculateMarchCapacity(march.troops);
        const victorySeverity = combatResult.defenderSurvivors.reduce((sum, t) => sum + t.quantity, 0) === 0 ? 1.0 : 0.7;
        loot = calculateLoot(npcResources, capacity, victorySeverity);
        march.resources = loot;
      }

      // Update march with survivors
      march.troops = combatResult.attackerSurvivors;

      // Save battle report to database
      const battleId = crypto.randomUUID();
      await this.env.DB.prepare(`
        INSERT INTO battle_reports (battle_id, attacker_id, defender_id, timestamp, winner, attacker_losses, defender_losses, loot, combat_log)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
      `).bind(
        battleId,
        this.playerState.playerId,
        'npc_' + npcCamp.camp_id,
        Date.now(),
        combatResult.winner,
        JSON.stringify(combatResult.attackerLosses),
        JSON.stringify(combatResult.defenderLosses),
        loot ? JSON.stringify(loot) : null,
        JSON.stringify(combatResult.rounds)
      ).run();

      // Send battle report message to player
      await this.sendMessage({
        recipient_id: this.playerState.playerId,
        sender_id: null,
        sender_name: 'Battle System',
        message_type: 'battle_report',
        subject: `Battle Report: ${npcCamp.camp_type} Camp Lv.${npcCamp.level}`,
        body: `Your forces ${combatResult.winner === 'attacker' ? 'defeated' : 'were defeated by'} the ${npcCamp.camp_type} camp.`,
        metadata: JSON.stringify({ battleId, marchId: march.marchId })
      });

      march.status = 'returning';

      // Calculate return time
      const marchSpeed = this.playerState.research['logistics'] || 0;
      const returnTime = calculateMarchTime(
        calculateDistance(march.destination, march.origin),
        march.troops,
        marchSpeed * 10
      );

      march.returnTime = Date.now() + (returnTime * 1000);

      this.pushEvent('march_arrived', {
        marchId: march.marchId,
        combatResult: combatResult.winner,
        loot,
        returnTime: march.returnTime
      });

      console.log(`[PlayerDO] March ${march.marchId} ${combatResult.winner === 'attacker' ? 'won' : 'lost'} battle, returning`);
    } else if (march.marchType === 'attack' && march.targetType === 'player') {
      // PvP Combat
      // Query target player's city tile
      const targetTile = await this.env.DB.prepare(
        'SELECT owner_id FROM world_tiles WHERE x = ? AND y = ? AND tile_type = ?'
      ).bind(march.destination.x, march.destination.y, 'city').first();

      if (!targetTile || !targetTile.owner_id) {
        console.error(`[PlayerDO] No player city found at ${march.destination.x},${march.destination.y}`);
        march.status = 'returning';
        march.returnTime = Date.now() + 10000;
        return;
      }

      const targetPlayerId = targetTile.owner_id as string;

      // Get target player's Durable Object
      const targetPlayerDO = this.env.PLAYER_DO.get(this.env.PLAYER_DO.idFromName(targetPlayerId));
      const targetStateResponse = await targetPlayerDO.fetch(new Request('https://fake/api/player/state'));
      const targetState = await targetStateResponse.json() as any;

      if (!targetState) {
        console.error(`[PlayerDO] Could not load target player state for ${targetPlayerId}`);
        march.status = 'returning';
        march.returnTime = Date.now() + 10000;
        return;
      }

      // Convert march troops to combat format
      const attackerTroops: CombatTroop[] = march.troops.map(t => ({
        troopType: t.troopType,
        quantity: t.quantity
      }));

      // Get defender's wall troops (only troops stationed on wall defend)
      const defenderTroops: CombatTroop[] = (targetState.troops || [])
        .filter((t: any) => t.location === 'wall')
        .map((t: any) => ({
          troopType: t.troopType,
          quantity: t.quantity
        }));

      // Prepare combat sides
      const attacker: CombatSide = {
        playerId: this.playerState.playerId,
        playerName: this.playerState.playerName,
        troops: attackerTroops,
        research: this.playerState.research,
        dragonBonus: 0 // TODO: Add dragon bonuses
      };

      const defender: CombatSide = {
        playerId: targetPlayerId,
        playerName: targetState.playerName || 'Unknown Player',
        troops: defenderTroops,
        research: targetState.research || {},
        dragonBonus: 0 // TODO: Add dragon bonuses
      };

      // Resolve combat
      const combatResult = resolveCombat(attacker, defender);

      // Calculate plunder if attacker won
      let loot = null;
      if (combatResult.winner === 'attacker') {
        const capacity = calculateMarchCapacity(march.troops);
        const defenderResources = {
          food: Math.floor((targetState.resources?.food || 0) * 0.1), // Can plunder 10% of unprotected resources
          wood: Math.floor((targetState.resources?.wood || 0) * 0.1),
          stone: Math.floor((targetState.resources?.stone || 0) * 0.1),
          metal: Math.floor((targetState.resources?.metal || 0) * 0.1),
          gold: Math.floor((targetState.resources?.gold || 0) * 0.1)
        };

        const victorySeverity = combatResult.defenderSurvivors.reduce((sum, t) => sum + t.quantity, 0) === 0 ? 1.0 : 0.7;
        loot = calculateLoot(defenderResources, capacity, victorySeverity);
        march.resources = loot;

        // Deduct resources from defender (notify via their DO)
        await targetPlayerDO.fetch(new Request('https://fake/internal/plunder', {
          method: 'POST',
          body: JSON.stringify({
            loot,
            attackerId: this.playerState.playerId,
            losses: combatResult.defenderLosses
          })
        }));
      }

      // Update march with survivors
      march.troops = combatResult.attackerSurvivors;

      // Save battle report to database
      const battleId = crypto.randomUUID();
      await this.env.DB.prepare(`
        INSERT INTO battle_reports (battle_id, attacker_id, defender_id, timestamp, winner, attacker_losses, defender_losses, loot, combat_log)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
      `).bind(
        battleId,
        this.playerState.playerId,
        targetPlayerId,
        Date.now(),
        combatResult.winner,
        JSON.stringify(combatResult.attackerLosses),
        JSON.stringify(combatResult.defenderLosses),
        loot ? JSON.stringify(loot) : null,
        JSON.stringify(combatResult.rounds)
      ).run();

      // Send battle report to attacker
      await this.sendMessage({
        recipient_id: this.playerState.playerId,
        sender_id: null,
        sender_name: 'Battle System',
        message_type: 'battle_report',
        subject: `Battle Report: Attack on ${targetState.playerName}`,
        body: `Your forces ${combatResult.winner === 'attacker' ? 'defeated' : 'were defeated by'} ${targetState.playerName}.`,
        metadata: JSON.stringify({ battleId, marchId: march.marchId, isAttacker: true })
      });

      // Send battle report to defender
      await this.sendMessage({
        recipient_id: targetPlayerId,
        sender_id: null,
        sender_name: 'Battle System',
        message_type: 'battle_report',
        subject: `Battle Report: Defended against ${this.playerState.playerName}`,
        body: `${this.playerState.playerName} attacked your city. ${combatResult.winner === 'defender' ? 'Your defenses held!' : 'Your city was plundered!'}`,
        metadata: JSON.stringify({ battleId, isAttacker: false })
      });

      march.status = 'returning';

      // Calculate return time
      const marchSpeed = this.playerState.research['logistics'] || 0;
      const returnTime = calculateMarchTime(
        calculateDistance(march.destination, march.origin),
        march.troops,
        marchSpeed * 10
      );

      march.returnTime = Date.now() + (returnTime * 1000);

      this.pushEvent('march_arrived', {
        marchId: march.marchId,
        combatResult: combatResult.winner,
        loot,
        returnTime: march.returnTime
      });

      console.log(`[PlayerDO] PvP March ${march.marchId} ${combatResult.winner === 'attacker' ? 'won' : 'lost'} battle against ${targetState.playerName}, returning`);
    } else if (march.marchType === 'gather') {
      // Gathering from wilderness
      const capacity = calculateMarchCapacity(march.troops);

      // Simulate gathering (50% of capacity)
      const gatherAmount = Math.floor(capacity * 0.5);
      march.resources = {
        food: Math.floor(gatherAmount * 0.4),
        wood: Math.floor(gatherAmount * 0.3),
        stone: Math.floor(gatherAmount * 0.2),
        metal: Math.floor(gatherAmount * 0.1)
      };

      march.status = 'returning';

      const marchSpeed = this.playerState.research['logistics'] || 0;
      const returnTime = calculateMarchTime(
        calculateDistance(march.destination, march.origin),
        march.troops,
        marchSpeed * 10
      );

      march.returnTime = Date.now() + (returnTime * 1000);

      this.pushEvent('march_arrived', {
        marchId: march.marchId,
        resources: march.resources,
        returnTime: march.returnTime
      });
    } else {
      // Other march types (scout, reinforce, transport)
      march.status = 'at_target';

      this.pushEvent('march_arrived', {
        marchId: march.marchId,
        status: 'at_target'
      });
    }
  }

  /**
   * Process march return to origin
   */
  private async processMarchReturn(march: March): Promise<void> {
    if (!this.playerState) return;

    console.log(`[PlayerDO] March ${march.marchId} returned home`);

    // Return troops to player
    for (const marchTroop of march.troops) {
      const existingTroop = this.playerState.troops.find(t => t.troopType === marchTroop.troopType);
      if (existingTroop) {
        existingTroop.quantity += marchTroop.quantity;
      } else {
        this.playerState.troops.push({
          troopType: marchTroop.troopType,
          quantity: marchTroop.quantity,
          location: 'home'
        });
      }
    }

    // Add resources if any
    if (march.resources) {
      this.playerState.resources.food += march.resources.food || 0;
      this.playerState.resources.wood += march.resources.wood || 0;
      this.playerState.resources.stone += march.resources.stone || 0;
      this.playerState.resources.metal += march.resources.metal || 0;
      this.playerState.resources.gold += march.resources.gold || 0;

      this.pushEvent('march_returned', {
        marchId: march.marchId,
        troops: march.troops,
        resources: march.resources
      });
    } else {
      this.pushEvent('march_returned', {
        marchId: march.marchId,
        troops: march.troops
      });
    }

    // Remove march from active marches
    march.status = 'completed';
    this.playerState.activeMarches = this.playerState.activeMarches.filter(
      m => m.marchId !== march.marchId
    );
  }

  /**
   * Get player messages (inbox)
   */
  private async handleGetMessages(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const url = new URL(request.url);
    const limit = parseInt(url.searchParams.get('limit') || '20');
    const offset = parseInt(url.searchParams.get('offset') || '0');
    const unreadOnly = url.searchParams.get('unreadOnly') === 'true';

    let query = 'SELECT * FROM messages WHERE recipient_id = ?';
    const params: any[] = [this.playerState.playerId];

    if (unreadOnly) {
      query += ' AND is_read = 0';
    }

    query += ' ORDER BY timestamp DESC LIMIT ? OFFSET ?';
    params.push(limit, offset);

    const result = await this.env.DB.prepare(query)
      .bind(...params)
      .all();

    return new Response(JSON.stringify({
      messages: result.results,
      total: result.results?.length || 0
    }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Mark message as read
   */
  private async handleMarkMessageRead(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as { messageId: string };

    await this.env.DB.prepare(
      'UPDATE messages SET is_read = 1 WHERE message_id = ? AND recipient_id = ?'
    ).bind(body.messageId, this.playerState.playerId).run();

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  /**
   * Send message to player (battle reports, system messages, etc.)
   */
  private async sendMessage(message: {
    recipient_id: string;
    sender_id: string | null;
    sender_name: string;
    message_type: string;
    subject: string;
    body: string;
    metadata: string;
  }): Promise<void> {
    const messageId = crypto.randomUUID();
    const timestamp = Date.now();

    await this.env.DB.prepare(`
      INSERT INTO messages (message_id, recipient_id, sender_id, sender_name, message_type, subject, body, metadata, timestamp)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    `).bind(
      messageId,
      message.recipient_id,
      message.sender_id,
      message.sender_name,
      message.message_type,
      message.subject,
      message.body,
      message.metadata,
      timestamp
    ).run();

    // Notify player via WebSocket
    this.pushEvent('new_message', {
      messageId,
      messageType: message.message_type,
      subject: message.subject
    });
  }

  /**
   * Handle plundering from PvP combat (called by attacker's DO)
   */
  private async handlePlunder(request: Request): Promise<Response> {
    if (!this.playerState) {
      return this.errorResponse('State not loaded', 500);
    }

    const body = await request.json() as {
      loot: { food?: number; wood?: number; stone?: number; metal?: number; gold?: number };
      attackerId: string;
      losses: Array<{ troopType: string; quantity: number }>;
    };

    // Deduct resources
    if (body.loot) {
      this.playerState.resources.food = Math.max(0, this.playerState.resources.food - (body.loot.food || 0));
      this.playerState.resources.wood = Math.max(0, this.playerState.resources.wood - (body.loot.wood || 0));
      this.playerState.resources.stone = Math.max(0, this.playerState.resources.stone - (body.loot.stone || 0));
      this.playerState.resources.metal = Math.max(0, this.playerState.resources.metal - (body.loot.metal || 0));
      this.playerState.resources.gold = Math.max(0, this.playerState.resources.gold - (body.loot.gold || 0));
    }

    // Deduct troop losses (only from wall - city troops are safe)
    for (const loss of body.losses) {
      const wallTroop = this.playerState.troops.find(
        t => t.troopType === loss.troopType && t.location === 'wall'
      );
      if (wallTroop) {
        wallTroop.quantity = Math.max(0, wallTroop.quantity - loss.quantity);
        if (wallTroop.quantity === 0) {
          this.playerState.troops = this.playerState.troops.filter(t => t !== wallTroop);
        }
      }
    }

    await this.saveState();

    // Notify player via WebSocket
    this.pushEvent('plundered', {
      attackerId: body.attackerId,
      loot: body.loot,
      losses: body.losses
    });

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' }
    });
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
