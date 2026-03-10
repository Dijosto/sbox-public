/**
 * World Region Durable Object
 * Manages a 100x100 tile region of the world map
 * Handles march scheduling, combat triggers, and viewport queries
 */

import { Env } from '../workers/index';

export class WorldRegionDurableObject {
  private state: DurableObjectState;
  private env: Env;
  private tiles: Map<string, any> = new Map();

  constructor(state: DurableObjectState, env: Env) {
    this.state = state;
    this.env = env;
  }

  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === '/viewport') {
      return this.handleViewport(url);
    }

    if (url.pathname === '/tile') {
      return this.handleTile(url);
    }

    return new Response('Not Found', { status: 404 });
  }

  private async handleViewport(url: URL): Promise<Response> {
    const minX = parseInt(url.searchParams.get('minX') || '0');
    const maxX = parseInt(url.searchParams.get('maxX') || '0');
    const minY = parseInt(url.searchParams.get('minY') || '0');
    const maxY = parseInt(url.searchParams.get('maxY') || '0');

    const tiles = [];

    // Generate sample tiles (in production, load from storage/D1)
    for (let x = minX; x <= maxX; x++) {
      for (let y = minY; y <= maxY; y++) {
        tiles.push({
          position: { x, y },
          tileType: 'empty',
          level: 0,
        });
      }
    }

    const viewport = {
      viewportMin: { x: minX, y: minY },
      viewportMax: { x: maxX, y: maxY },
      tiles,
      marches: [],
    };

    return new Response(JSON.stringify(viewport), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  private async handleTile(url: URL): Promise<Response> {
    const x = parseInt(url.searchParams.get('x') || '0');
    const y = parseInt(url.searchParams.get('y') || '0');

    const tile = {
      position: { x, y },
      tileType: 'empty',
      level: 0,
    };

    return new Response(JSON.stringify(tile), {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  async alarm(): Promise<void> {
    // Handle march arrivals
    console.log('World Region alarm triggered');
  }
}
