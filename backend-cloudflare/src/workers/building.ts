import { Env } from './index';

export class BuildingHandler {
  static async handle(request: Request, env: Env): Promise<Response> {
    // Verify authentication and forward to Player DO
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

    // Get Player Durable Object and forward request
    const id = env.PLAYER_DO.idFromName(playerId);
    const stub = env.PLAYER_DO.get(id);

    return stub.fetch(request);
  }

  private static async verifyToken(token: string, env: Env): Promise<string | null> {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      const payload = JSON.parse(atob(parts[1]));

      if (payload.exp && payload.exp < Date.now() / 1000) {
        return null;
      }

      return payload.playerId;
    } catch {
      return null;
    }
  }
}
