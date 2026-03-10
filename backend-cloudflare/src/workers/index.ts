/**
 * Main Cloudflare Worker entry point
 * Routes requests to appropriate handlers
 */

import { AuthHandler } from './auth';
import { PlayerHandler } from './player';
import { BuildingHandler } from './building';
import { WorldHandler } from './world';
import { AllianceHandler } from './alliance';

export interface Env {
  // Durable Objects
  PLAYER_DO: DurableObjectNamespace;
  ALLIANCE_DO: DurableObjectNamespace;
  WORLD_REGION_DO: DurableObjectNamespace;
  COMBAT_DO: DurableObjectNamespace;

  // D1 Database
  DB: D1Database;

  // KV for game config
  GAME_CONFIG: KVNamespace;

  // Queue for async tasks
  GAME_QUEUE: Queue;

  // Environment variables
  ENVIRONMENT: string;
  JWT_SECRET: string;
  STEAM_API_KEY: string;

  // Test mode configuration (local dev only)
  // Set to value like "100" to make everything 100x faster for testing
  // MUST NOT be set in production - cheating risk
  TEST_SPEED_MULTIPLIER?: string;
}

export default {
  async fetch(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
    const url = new URL(request.url);

    // CORS headers
    const corsHeaders = {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization, X-Client-Timestamp',
    };

    // Handle CORS preflight
    if (request.method === 'OPTIONS') {
      return new Response(null, { headers: corsHeaders });
    }

    try {
      // Route requests
      let response: Response;

      if (url.pathname.startsWith('/api/auth')) {
        response = await AuthHandler.handle(request, env);
      } else if (url.pathname.startsWith('/api/player')) {
        response = await PlayerHandler.handle(request, env);
      } else if (url.pathname.startsWith('/api/building')) {
        response = await BuildingHandler.handle(request, env);
      } else if (url.pathname.startsWith('/api/world')) {
        response = await WorldHandler.handle(request, env);
      } else if (url.pathname.startsWith('/api/alliance')) {
        response = await AllianceHandler.handle(request, env);
      } else if (url.pathname === '/ws') {
        // WebSocket upgrade
        return await handleWebSocketUpgrade(request, env);
      } else if (url.pathname === '/health') {
        response = new Response(JSON.stringify({ status: 'ok' }), {
          headers: { 'Content-Type': 'application/json' },
        });
      } else {
        response = new Response('Not Found', { status: 404 });
      }

      // Add CORS headers to response
      const newHeaders = new Headers(response.headers);
      Object.entries(corsHeaders).forEach(([key, value]) => {
        newHeaders.set(key, value);
      });

      return new Response(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers: newHeaders,
      });
    } catch (error) {
      console.error('Worker error:', error);
      return new Response(JSON.stringify({ error: 'Internal Server Error' }), {
        status: 500,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }
  },

  async queue(batch: MessageBatch<any>, env: Env): Promise<void> {
    // Handle queued messages (combat results, construction completion, etc.)
    for (const message of batch.messages) {
      try {
        await handleQueueMessage(message.body, env);
        message.ack();
      } catch (error) {
        console.error('Queue message error:', error);
        message.retry();
      }
    }
  },
};

async function handleWebSocketUpgrade(request: Request, env: Env): Promise<Response> {
  const upgrade = request.headers.get('Upgrade');
  if (upgrade !== 'websocket') {
    return new Response('Expected WebSocket', { status: 400 });
  }

  // Extract JWT token from query string
  const url = new URL(request.url);
  const token = url.searchParams.get('token');

  if (!token) {
    return new Response('Missing token', { status: 401 });
  }

  // Verify token and get player ID
  const playerId = await verifyJWT(token, env);
  if (!playerId) {
    return new Response('Invalid token', { status: 401 });
  }

  // Get Player Durable Object
  const id = env.PLAYER_DO.idFromName(playerId);
  const stub = env.PLAYER_DO.get(id);

  // Forward WebSocket to Durable Object
  return stub.fetch(request);
}

async function verifyJWT(token: string, env: Env): Promise<string | null> {
  // Simple JWT verification (in production, use a proper library)
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const payload = JSON.parse(atob(parts[1]));

    // Check expiration
    if (payload.exp && payload.exp < Date.now() / 1000) {
      return null;
    }

    return payload.playerId;
  } catch {
    return null;
  }
}

async function handleQueueMessage(message: any, env: Env): Promise<void> {
  switch (message.type) {
    case 'combat_result':
      await handleCombatResult(message, env);
      break;
    case 'building_complete':
      await handleBuildingComplete(message, env);
      break;
    case 'training_complete':
      await handleTrainingComplete(message, env);
      break;
    default:
      console.warn('Unknown queue message type:', message.type);
  }
}

async function handleCombatResult(message: any, env: Env): Promise<void> {
  // Notify both players of combat result
  const attackerId = message.attackerId;
  const defenderId = message.defenderId;

  // Update attacker state
  const attackerDO = env.PLAYER_DO.get(env.PLAYER_DO.idFromName(attackerId));
  await attackerDO.fetch(new Request('https://fake/combat_result', {
    method: 'POST',
    body: JSON.stringify(message.result),
  }));

  // Update defender state
  const defenderDO = env.PLAYER_DO.get(env.PLAYER_DO.idFromName(defenderId));
  await defenderDO.fetch(new Request('https://fake/combat_result', {
    method: 'POST',
    body: JSON.stringify(message.result),
  }));
}

async function handleBuildingComplete(message: any, env: Env): Promise<void> {
  const playerId = message.playerId;
  const playerDO = env.PLAYER_DO.get(env.PLAYER_DO.idFromName(playerId));

  await playerDO.fetch(new Request('https://fake/building_complete', {
    method: 'POST',
    body: JSON.stringify(message),
  }));
}

async function handleTrainingComplete(message: any, env: Env): Promise<void> {
  const playerId = message.playerId;
  const playerDO = env.PLAYER_DO.get(env.PLAYER_DO.idFromName(playerId));

  await playerDO.fetch(new Request('https://fake/training_complete', {
    method: 'POST',
    body: JSON.stringify(message),
  }));
}

// Export Durable Object classes
export { PlayerDurableObject } from '../durable-objects/PlayerDO';
export { AllianceDurableObject } from '../durable-objects/AllianceDO';
export { WorldRegionDurableObject } from '../durable-objects/WorldRegionDO';
export { CombatDurableObject } from '../durable-objects/CombatDO';
