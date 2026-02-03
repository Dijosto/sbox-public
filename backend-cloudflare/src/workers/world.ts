import { Env } from './index';

export class WorldHandler {
  static async handle(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    // GET /api/world/tiles?regionX=3&regionY=4 - Query tiles in a region
    if (url.pathname === '/api/world/tiles' && request.method === 'GET') {
      return await this.getTiles(request, env);
    }

    // GET /api/world/tile/:x/:y - Get specific tile
    const tileMatch = url.pathname.match(/^\/api\/world\/tile\/(\d+)\/(\d+)$/);
    if (tileMatch && request.method === 'GET') {
      const x = parseInt(tileMatch[1]);
      const y = parseInt(tileMatch[2]);
      return await this.getTile(x, y, env);
    }

    // Legacy: GET /api/world/tile?x=123&y=456
    if (url.pathname === '/api/world/tile' && request.method === 'GET') {
      const x = parseInt(url.searchParams.get('x') || '0');
      const y = parseInt(url.searchParams.get('y') || '0');
      return await this.getTile(x, y, env);
    }

    // GET /api/world/search?playerName=... - Search for player cities
    if (url.pathname === '/api/world/search' && request.method === 'GET') {
      return await this.searchPlayers(request, env);
    }

    return new Response('Not Found', { status: 404 });
  }

  /**
   * Get tiles in a region (100x100 grid)
   * Query params: regionX, regionY, includeEmpty (default false)
   */
  private static async getTiles(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const regionX = parseInt(url.searchParams.get('regionX') || '0');
    const regionY = parseInt(url.searchParams.get('regionY') || '0');
    const includeEmpty = url.searchParams.get('includeEmpty') === 'true';

    // Calculate region bounds (regions are 100x100)
    const minX = regionX * 100;
    const maxX = minX + 100;
    const minY = regionY * 100;
    const maxY = minY + 100;

    // Query tiles in region
    let query = `
      SELECT x, y, tile_type, owner_id, level, resource_type, resource_bonus
      FROM world_tiles
      WHERE x >= ? AND x < ? AND y >= ? AND y < ?
    `;

    if (!includeEmpty) {
      query += ` AND tile_type != 'empty'`;
    }

    query += ` LIMIT 1000`; // Safety limit

    const result = await env.DB.prepare(query)
      .bind(minX, maxX, minY, maxY)
      .all();

    // For cities, fetch player names
    const cityTiles = result.results.filter((t: any) => t.tile_type === 'city');
    const playerIds = cityTiles.map((t: any) => t.owner_id).filter(Boolean);

    let playerNames: Record<string, string> = {};
    if (playerIds.length > 0) {
      const placeholders = playerIds.map(() => '?').join(',');
      const playersResult = await env.DB.prepare(
        `SELECT player_id, name FROM players WHERE player_id IN (${placeholders})`
      ).bind(...playerIds).all();

      playersResult.results.forEach((p: any) => {
        playerNames[p.player_id] = p.name;
      });
    }

    // Enrich tiles with player names
    const enrichedTiles = result.results.map((tile: any) => ({
      x: tile.x,
      y: tile.y,
      tileType: tile.tile_type,
      ownerId: tile.owner_id,
      ownerName: tile.tile_type === 'city' && tile.owner_id ? playerNames[tile.owner_id] : null,
      level: tile.level,
      resourceType: tile.resource_type,
      resourceBonus: tile.resource_bonus
    }));

    return new Response(JSON.stringify({
      region: { x: regionX, y: regionY },
      tiles: enrichedTiles,
      count: enrichedTiles.length
    }), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Get specific tile details
   */
  private static async getTile(x: number, y: number, env: Env): Promise<Response> {
    const tile = await env.DB.prepare(
      'SELECT * FROM world_tiles WHERE x = ? AND y = ?'
    ).bind(x, y).first();

    if (!tile) {
      return new Response(JSON.stringify({
        x,
        y,
        tileType: 'empty',
        message: 'Empty tile - no data'
      }), {
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const tileData: any = {
      x: tile.x,
      y: tile.y,
      tileType: tile.tile_type,
      level: tile.level,
      resourceType: tile.resource_type,
      resourceBonus: tile.resource_bonus
    };

    // If it's a city, get player info
    if (tile.tile_type === 'city' && tile.owner_id) {
      const player = await env.DB.prepare(
        'SELECT player_id, name FROM players WHERE player_id = ?'
      ).bind(tile.owner_id).first();

      if (player) {
        tileData.ownerId = player.player_id;
        tileData.ownerName = player.name;
      }
    }

    // If it's an NPC camp, get camp details
    if (tile.tile_type === 'npc_camp') {
      const camp = await env.DB.prepare(
        'SELECT * FROM npc_camps WHERE x = ? AND y = ?'
      ).bind(x, y).first();

      if (camp) {
        tileData.campId = camp.camp_id;
        tileData.campType = camp.camp_type;
        tileData.level = camp.level;
        tileData.maxAttacksPerDay = camp.max_attacks_per_day;
        // Don't expose garrison details - players need to scout
        // tileData.garrison = JSON.parse(camp.garrison as string);
      }
    }

    return new Response(JSON.stringify(tileData), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Search for player cities by name
   */
  private static async searchPlayers(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const playerName = url.searchParams.get('playerName');

    if (!playerName || playerName.length < 3) {
      return new Response(JSON.stringify({
        error: 'Player name must be at least 3 characters'
      }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Search players by name
    const players = await env.DB.prepare(
      'SELECT player_id, name FROM players WHERE name LIKE ? LIMIT 50'
    ).bind(`%${playerName}%`).all();

    if (players.results.length === 0) {
      return new Response(JSON.stringify({
        results: []
      }), {
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Get city locations for found players
    const playerIds = players.results.map((p: any) => p.player_id);
    const placeholders = playerIds.map(() => '?').join(',');

    const cities = await env.DB.prepare(
      `SELECT x, y, owner_id FROM world_tiles WHERE owner_id IN (${placeholders}) AND tile_type = 'city'`
    ).bind(...playerIds).all();

    // Map cities to players
    const cityMap: Record<string, { x: number; y: number }> = {};
    cities.results.forEach((city: any) => {
      cityMap[city.owner_id] = { x: city.x, y: city.y };
    });

    const results = players.results.map((player: any) => ({
      playerId: player.player_id,
      playerName: player.name,
      cityLocation: cityMap[player.player_id] || null
    }));

    return new Response(JSON.stringify({
      results
    }), {
      headers: { 'Content-Type': 'application/json' }
    });
  }
}
