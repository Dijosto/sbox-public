import { Env } from './index';

export class PlayerHandler {
  static async handle(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    // Initialize endpoint doesn't require authentication
    if (url.pathname === '/api/player/initialize') {
      // Extract playerId from request body
      const body = await request.json() as { playerId: string };
      const playerId = body.playerId;

      if (!playerId) {
        return new Response(JSON.stringify({ error: 'playerId required' }), {
          status: 400,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      // Get Player Durable Object
      const id = env.PLAYER_DO.idFromName(playerId);
      const stub = env.PLAYER_DO.get(id);

      // Forward request to Durable Object (recreate request with body)
      return stub.fetch(new Request(request.url, {
        method: request.method,
        headers: request.headers,
        body: JSON.stringify(body),
      }));
    }

    // All other endpoints require authentication
    const authHeader = request.headers.get('Authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return new Response(JSON.stringify({ error: 'Unauthorized' }), {
        status: 401,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    const token = authHeader.substring(7);
    const playerId = await this.verifyToken(token, env);

    if (!playerId) {
      return new Response(JSON.stringify({ error: 'Invalid token' }), {
        status: 401,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    // Get Player Durable Object
    const id = env.PLAYER_DO.idFromName(playerId);
    const stub = env.PLAYER_DO.get(id);

    // Forward request to Durable Object
    return stub.fetch(request);
  }

  private static async verifyToken(token: string, env: Env): Promise<string | null> {
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
}
