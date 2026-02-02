import { Env } from './index';

export class AuthHandler {
  static async handle(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === '/api/auth/steam' && request.method === 'POST') {
      return await this.authenticateSteam(request, env);
    }

    return new Response('Not Found', { status: 404 });
  }

  private static async authenticateSteam(request: Request, env: Env): Promise<Response> {
    try {
      const body = await request.json() as any;
      const steamId = body.steamId;

      if (!steamId) {
        return new Response(JSON.stringify({ success: false, error: 'Missing Steam ID' }), {
          status: 400,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      // TODO: Verify Steam auth ticket with Steam API
      // For now, just accept the Steam ID

      // Check if player exists in database
      const player = await env.DB.prepare(
        'SELECT player_id, name FROM players WHERE steam_id = ?'
      )
        .bind(steamId)
        .first();

      let playerId: string;
      let playerName: string;

      if (!player) {
        // Create new player
        playerId = crypto.randomUUID();
        playerName = `Player_${steamId.substring(0, 8)}`;

        await env.DB.prepare(
          'INSERT INTO players (player_id, steam_id, name, created_at) VALUES (?, ?, ?, ?)'
        )
          .bind(playerId, steamId, playerName, Date.now())
          .run();

        // Initialize player state in Durable Object
        const playerDO = env.PLAYER_DO.get(env.PLAYER_DO.idFromName(playerId));
        await playerDO.fetch(new Request('https://fake/initialize', {
          method: 'POST',
          body: JSON.stringify({ playerId, playerName, steamId }),
        }));
      } else {
        playerId = player.player_id as string;
        playerName = player.name as string;
      }

      // Generate JWT token
      const token = await this.generateJWT(playerId, env);

      return new Response(JSON.stringify({
        success: true,
        token,
        playerId,
        playerName,
      }), {
        headers: { 'Content-Type': 'application/json' },
      });
    } catch (error) {
      console.error('Auth error:', error);
      return new Response(JSON.stringify({ success: false, error: 'Authentication failed' }), {
        status: 500,
        headers: { 'Content-Type': 'application/json' },
      });
    }
  }

  private static async generateJWT(playerId: string, env: Env): Promise<string> {
    // Simple JWT generation (in production, use a proper library)
    const header = { alg: 'HS256', typ: 'JWT' };
    const payload = {
      playerId,
      exp: Math.floor(Date.now() / 1000) + (7 * 24 * 60 * 60), // 7 days
    };

    const encodedHeader = btoa(JSON.stringify(header));
    const encodedPayload = btoa(JSON.stringify(payload));

    // In production, implement proper HMAC signature
    const signature = btoa(`${env.JWT_SECRET}:${encodedPayload}`);

    return `${encodedHeader}.${encodedPayload}.${signature}`;
  }
}
