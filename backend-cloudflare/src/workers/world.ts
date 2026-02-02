import { Env } from './index';

export class WorldHandler {
  static async handle(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === '/api/world/viewport') {
      return this.handleViewport(request, env);
    }

    if (url.pathname === '/api/world/tile') {
      return this.handleTile(request, env);
    }

    return new Response('Not Found', { status: 404 });
  }

  private static async handleViewport(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const minX = parseInt(url.searchParams.get('minX') || '0');
    const maxX = parseInt(url.searchParams.get('maxX') || '0');
    const minY = parseInt(url.searchParams.get('minY') || '0');
    const maxY = parseInt(url.searchParams.get('maxY') || '0');

    // Get region DO
    const regionX = Math.floor(minX / 100);
    const regionY = Math.floor(minY / 100);

    const id = env.WORLD_REGION_DO.idFromName(`region_${regionX}_${regionY}`);
    const stub = env.WORLD_REGION_DO.get(id);

    return stub.fetch(new Request(`https://fake/viewport?minX=${minX}&maxX=${maxX}&minY=${minY}&maxY=${maxY}`));
  }

  private static async handleTile(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const x = parseInt(url.searchParams.get('x') || '0');
    const y = parseInt(url.searchParams.get('y') || '0');

    const regionX = Math.floor(x / 100);
    const regionY = Math.floor(y / 100);

    const id = env.WORLD_REGION_DO.idFromName(`region_${regionX}_${regionY}`);
    const stub = env.WORLD_REGION_DO.get(id);

    return stub.fetch(new Request(`https://fake/tile?x=${x}&y=${y}`));
  }
}
