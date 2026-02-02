/**
 * Player Durable Object
 * Manages all player state: city, resources, troops, research, etc.
 * One instance per player, persists in-memory state with transactional storage
 */

import { Env } from '../workers/index';

interface PlayerState {
  playerId: string;
  playerName: string;
  city: CityState;
  resources: Resources;
  research: Record<string, number>;
  troops: TroopStack[];
  activeMarches: March[];
  lastUpdateTimestamp: number;
}

interface CityState {
  position: { x: number; y: number };
  innerCity: Record<string, BuildingState>;
  outerFields: Record<string, BuildingState>;
  buildQueue: BuildQueueItem[];
  trainQueue: TrainQueueItem[];
  maxWorkers: number;
  usedWorkers: number;
}

interface BuildingState {
  buildingType: string;
  level: number;
  completionTime?: number;
}

interface BuildQueueItem {
  queueId: string;
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

interface Resources {
  food: number;
  wood: number;
  stone: number;
  metal: number;
  gold: number;
  premiumCurrency: number;
  foodRate: number;
  woodRate: number;
  stoneRate: number;
  metalRate: number;
  goldRate: number;
  foodCap: number;
  woodCap: number;
  stoneCap: number;
  metalCap: number;
  goldCap: number;
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

    // Route internal requests
    if (url.pathname === '/initialize') {
      return this.handleInitialize(request);
    }

    // API routes
    if (url.pathname === '/api/player/state') {
      return this.handleGetState(request);
    }

    return new Response('Not Found', { status: 404 });
  }

  private async loadState(): Promise<void> {
    const stored = await this.state.storage.get<PlayerState>('state');

    if (stored) {
      this.playerState = stored;

      // Calculate accumulated resources since last update
      this.updateResources();
    }
  }

  private async saveState(): Promise<void> {
    if (this.playerState) {
      await this.state.storage.put('state', this.playerState);
    }
  }

  private updateResources(): void {
    if (!this.playerState) return;

    const now = Date.now();
    const elapsed = (now - this.playerState.lastUpdateTimestamp) / (1000 * 60 * 60); // hours

    const res = this.playerState.resources;

    res.food = Math.min(res.foodCap, res.food + res.foodRate * elapsed);
    res.wood = Math.min(res.woodCap, res.wood + res.woodRate * elapsed);
    res.stone = Math.min(res.stoneCap, res.stone + res.stoneRate * elapsed);
    res.metal = Math.min(res.metalCap, res.metal + res.metalRate * elapsed);
    res.gold = Math.min(res.goldCap, res.gold + res.goldRate * elapsed);

    this.playerState.lastUpdateTimestamp = now;
  }

  private async handleInitialize(request: Request): Promise<Response> {
    const body = await request.json() as any;

    // Create initial player state
    this.playerState = {
      playerId: body.playerId,
      playerName: body.playerName,
      city: {
        position: { x: 500, y: 500 }, // TODO: Assign from world map
        innerCity: {
          town_hall: { buildingType: 'town_hall', level: 1 },
        },
        outerFields: {
          farm_1: { buildingType: 'farm', level: 1 },
        },
        buildQueue: [],
        trainQueue: [],
        maxWorkers: 1,
        usedWorkers: 0,
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
      lastUpdateTimestamp: Date.now(),
    };

    await this.saveState();

    return new Response(JSON.stringify({ success: true }), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  private async handleGetState(request: Request): Promise<Response> {
    if (!this.playerState) {
      return new Response(JSON.stringify({ error: 'State not loaded' }), {
        status: 500,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    // Update resources before returning
    this.updateResources();
    await this.saveState();

    return new Response(JSON.stringify(this.playerState), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  private handleWebSocket(request: Request): Response {
    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);

    this.state.acceptWebSocket(server);
    this.websocket = server;

    // Send initial connection message
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
    // Handle incoming WebSocket messages (e.g., ping/pong)
    console.log('WebSocket message:', message);
  }

  async webSocketClose(ws: WebSocket, code: number, reason: string, wasClean: boolean): Promise<void> {
    this.websocket = null;
    console.log('WebSocket closed:', code, reason);
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
}
