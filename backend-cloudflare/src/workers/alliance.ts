import { Env } from './index';

/**
 * Alliance Handler
 * Routes alliance-related requests to the appropriate Alliance Durable Object
 */
export class AllianceHandler {
  /**
   * Handle alliance API requests
   */
  static async handle(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const path = url.pathname;

    // Extract JWT token
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

    // Route to appropriate handler
    if (path.includes('/alliance/create')) {
      return this.handleCreate(request, playerId, env);
    } else if (path.includes('/alliance/join')) {
      return this.handleJoin(request, playerId, env);
    } else if (path.includes('/alliance/leave')) {
      return this.handleLeave(request, playerId, env);
    } else if (path.includes('/alliance/invite')) {
      return this.handleInvite(request, playerId, env);
    } else if (path.includes('/alliance/accept-invite')) {
      return this.handleAcceptInvite(request, playerId, env);
    } else if (path.includes('/alliance/decline-invite')) {
      return this.handleDeclineInvite(request, playerId, env);
    } else if (path.includes('/alliance/list-invites')) {
      return this.handleListInvites(request, playerId, env);
    } else if (path.includes('/alliance/list')) {
      return this.handleListAlliances(request, env);
    } else if (path.includes('/alliance/info')) {
      return this.handleGetInfo(request, playerId, env);
    } else if (path.includes('/alliance/members')) {
      return this.handleGetMembers(request, playerId, env);
    } else if (path.includes('/alliance/kick')) {
      return this.handleKickMember(request, playerId, env);
    } else if (path.includes('/alliance/promote')) {
      return this.handlePromoteMember(request, playerId, env);
    } else if (path.includes('/alliance/demote')) {
      return this.handleDemoteMember(request, playerId, env);
    } else if (path.includes('/alliance/chat')) {
      // Forward to Alliance DO for chat handling
      const body = await request.json() as { allianceId: string };
      return this.forwardToAllianceDO(body.allianceId, request, env);
    } else if (path.includes('/alliance/diplomacy')) {
      const body = await request.json() as { allianceId: string };
      return this.forwardToAllianceDO(body.allianceId, request, env);
    } else if (path.includes('/alliance/disband')) {
      return this.handleDisband(request, playerId, env);
    }

    return new Response(JSON.stringify({ error: 'Unknown endpoint' }), {
      status: 404,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Create a new alliance
   */
  private static async handleCreate(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      name: string;
      tag: string;
      description: string;
    };

    // Validate alliance name and tag
    if (!body.name || body.name.length < 3 || body.name.length > 30) {
      return new Response(JSON.stringify({ error: 'Alliance name must be 3-30 characters' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (!body.tag || body.tag.length < 2 || body.tag.length > 5) {
      return new Response(JSON.stringify({ error: 'Alliance tag must be 2-5 characters' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if player is already in an alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id, name FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (!playerData) {
      return new Response(JSON.stringify({ error: 'Player not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (playerData.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is already in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if alliance name or tag already exists
    const existingAlliance = await env.DB.prepare(
      'SELECT alliance_id FROM alliances WHERE LOWER(name) = LOWER(?) OR LOWER(tag) = LOWER(?)'
    ).bind(body.name, body.tag).first();

    if (existingAlliance) {
      return new Response(JSON.stringify({ error: 'Alliance name or tag already taken' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Create alliance
    const allianceId = crypto.randomUUID();

    // Insert into database
    await env.DB.prepare(
      'INSERT INTO alliances (alliance_id, name, tag, leader_id, created_at) VALUES (?, ?, ?, ?, ?)'
    ).bind(allianceId, body.name, body.tag, playerId, Date.now()).run();

    // Update player's alliance
    await env.DB.prepare(
      'UPDATE players SET alliance_id = ? WHERE player_id = ?'
    ).bind(allianceId, playerId).run();

    // Initialize Alliance Durable Object
    const allianceDOId = env.ALLIANCE_DO.idFromName(allianceId);
    const allianceDO = env.ALLIANCE_DO.get(allianceDOId);

    const doRequest = new Request('http://alliance-do/create', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        allianceId,
        name: body.name,
        tag: body.tag,
        description: body.description,
        leaderId: playerId,
        leaderName: playerData.name
      })
    });

    const doResponse = await allianceDO.fetch(doRequest);
    const result = await doResponse.json();

    return new Response(JSON.stringify({
      success: true,
      allianceId,
      alliance: result.alliance
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Join an alliance (via application)
   */
  private static async handleJoin(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      allianceId: string;
    };

    // Check if player is already in an alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (playerData?.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is already in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if alliance exists
    const alliance = await env.DB.prepare(
      'SELECT alliance_id FROM alliances WHERE alliance_id = ?'
    ).bind(body.allianceId).first();

    if (!alliance) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Create application
    const invitationId = crypto.randomUUID();
    await env.DB.prepare(
      'INSERT INTO alliance_invitations (invitation_id, alliance_id, player_id, invited_by, type, status, created_at, expires_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?)'
    ).bind(invitationId, body.allianceId, playerId, playerId, 'application', 'pending', Date.now(), Date.now() + 7 * 24 * 60 * 60 * 1000).run();

    return new Response(JSON.stringify({
      success: true,
      message: 'Application sent to alliance'
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Leave current alliance
   */
  private static async handleLeave(request: Request, playerId: string, env: Env): Promise<Response> {
    // Get player's alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (!playerData?.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is not in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if player is the leader
    const alliance = await env.DB.prepare(
      'SELECT leader_id FROM alliances WHERE alliance_id = ?'
    ).bind(playerData.alliance_id).first();

    if (alliance?.leader_id === playerId) {
      return new Response(JSON.stringify({ error: 'Alliance leader must transfer leadership or disband alliance before leaving' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Remove player from alliance in database
    await env.DB.prepare(
      'UPDATE players SET alliance_id = NULL WHERE player_id = ?'
    ).bind(playerId).run();

    // Remove from Alliance DO
    const allianceDOId = env.ALLIANCE_DO.idFromName(playerData.alliance_id);
    const allianceDO = env.ALLIANCE_DO.get(allianceDOId);

    const doRequest = new Request('http://alliance-do/member/remove', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId,
        removedBy: playerId
      })
    });

    await allianceDO.fetch(doRequest);

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Invite a player to alliance
   */
  private static async handleInvite(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      targetPlayerId: string;
    };

    // Get inviter's alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (!playerData?.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is not in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if target player exists and is not in an alliance
    const targetPlayer = await env.DB.prepare(
      'SELECT player_id, alliance_id FROM players WHERE player_id = ?'
    ).bind(body.targetPlayerId).first();

    if (!targetPlayer) {
      return new Response(JSON.stringify({ error: 'Target player not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (targetPlayer.alliance_id) {
      return new Response(JSON.stringify({ error: 'Target player is already in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Create invitation
    const invitationId = crypto.randomUUID();
    await env.DB.prepare(
      'INSERT INTO alliance_invitations (invitation_id, alliance_id, player_id, invited_by, type, status, created_at, expires_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?)'
    ).bind(invitationId, playerData.alliance_id, body.targetPlayerId, playerId, 'invite', 'pending', Date.now(), Date.now() + 7 * 24 * 60 * 60 * 1000).run();

    return new Response(JSON.stringify({
      success: true,
      message: 'Invitation sent'
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Accept an alliance invitation
   */
  private static async handleAcceptInvite(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      invitationId: string;
    };

    // Get invitation
    const invitation: any = await env.DB.prepare(
      'SELECT * FROM alliance_invitations WHERE invitation_id = ? AND player_id = ? AND status = ?'
    ).bind(body.invitationId, playerId, 'pending').first();

    if (!invitation) {
      return new Response(JSON.stringify({ error: 'Invitation not found or already processed' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if player is already in an alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id, name FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (playerData?.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is already in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Update player's alliance
    await env.DB.prepare(
      'UPDATE players SET alliance_id = ? WHERE player_id = ?'
    ).bind(invitation.alliance_id, playerId).run();

    // Mark invitation as accepted
    await env.DB.prepare(
      'UPDATE alliance_invitations SET status = ? WHERE invitation_id = ?'
    ).bind('accepted', body.invitationId).run();

    // Add to Alliance DO
    const allianceDOId = env.ALLIANCE_DO.idFromName(invitation.alliance_id);
    const allianceDO = env.ALLIANCE_DO.get(allianceDOId);

    const doRequest = new Request('http://alliance-do/member/add', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId,
        playerName: playerData.name,
        power: 0 // TODO: Calculate player power
      })
    });

    await allianceDO.fetch(doRequest);

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Decline an alliance invitation
   */
  private static async handleDeclineInvite(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      invitationId: string;
    };

    // Mark invitation as declined
    const result = await env.DB.prepare(
      'UPDATE alliance_invitations SET status = ? WHERE invitation_id = ? AND player_id = ? AND status = ?'
    ).bind('declined', body.invitationId, playerId, 'pending').run();

    if (!result.success || result.meta.changes === 0) {
      return new Response(JSON.stringify({ error: 'Invitation not found or already processed' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * List all pending invitations for a player
   */
  private static async handleListInvites(request: Request, playerId: string, env: Env): Promise<Response> {
    const invitations = await env.DB.prepare(
      'SELECT i.*, a.name as alliance_name, a.tag as alliance_tag FROM alliance_invitations i JOIN alliances a ON i.alliance_id = a.alliance_id WHERE i.player_id = ? AND i.status = ? ORDER BY i.created_at DESC'
    ).bind(playerId, 'pending').all();

    return new Response(JSON.stringify({
      invitations: invitations.results || []
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * List all alliances
   */
  private static async handleListAlliances(request: Request, env: Env): Promise<Response> {
    const alliances = await env.DB.prepare(
      'SELECT a.*, p.name as leader_name, COUNT(pm.player_id) as member_count FROM alliances a JOIN players p ON a.leader_id = p.player_id LEFT JOIN players pm ON pm.alliance_id = a.alliance_id GROUP BY a.alliance_id ORDER BY member_count DESC LIMIT 100'
    ).all();

    return new Response(JSON.stringify({
      alliances: alliances.results || []
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Get alliance information
   */
  private static async handleGetInfo(request: Request, playerId: string, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const allianceId = url.searchParams.get('allianceId');

    if (!allianceId) {
      return new Response(JSON.stringify({ error: 'Missing allianceId parameter' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Get from Alliance DO
    return this.forwardToAllianceDO(allianceId, request, env);
  }

  /**
   * Get alliance members
   */
  private static async handleGetMembers(request: Request, playerId: string, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const allianceId = url.searchParams.get('allianceId');

    if (!allianceId) {
      return new Response(JSON.stringify({ error: 'Missing allianceId parameter' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Get from Alliance DO
    return this.forwardToAllianceDO(allianceId, request, env);
  }

  /**
   * Kick a member from alliance
   */
  private static async handleKickMember(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      targetPlayerId: string;
      allianceId: string;
    };

    // Remove from database
    await env.DB.prepare(
      'UPDATE players SET alliance_id = NULL WHERE player_id = ?'
    ).bind(body.targetPlayerId).run();

    // Remove from Alliance DO
    return this.forwardToAllianceDO(body.allianceId, new Request(request.url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId: body.targetPlayerId,
        removedBy: playerId
      })
    }), env);
  }

  /**
   * Promote a member to officer
   */
  private static async handlePromoteMember(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      targetPlayerId: string;
      allianceId: string;
    };

    // Create new request with fresh body since we consumed the original
    const newRequest = new Request('http://alliance-do/member/promote', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId: body.targetPlayerId,
        promotedBy: playerId
      })
    });

    return this.forwardToAllianceDO(body.allianceId, newRequest, env);
  }

  /**
   * Demote an officer to member
   */
  private static async handleDemoteMember(request: Request, playerId: string, env: Env): Promise<Response> {
    const body = await request.json() as {
      targetPlayerId: string;
      allianceId: string;
    };

    // Create new request with fresh body since we consumed the original
    const newRequest = new Request('http://alliance-do/member/demote', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId: body.targetPlayerId,
        demotedBy: playerId
      })
    });

    return this.forwardToAllianceDO(body.allianceId, newRequest, env);
  }

  /**
   * Disband alliance
   */
  private static async handleDisband(request: Request, playerId: string, env: Env): Promise<Response> {
    // Get player's alliance
    const playerData = await env.DB.prepare(
      'SELECT alliance_id FROM players WHERE player_id = ?'
    ).bind(playerId).first();

    if (!playerData?.alliance_id) {
      return new Response(JSON.stringify({ error: 'Player is not in an alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check if player is the leader
    const alliance = await env.DB.prepare(
      'SELECT leader_id FROM alliances WHERE alliance_id = ?'
    ).bind(playerData.alliance_id).first();

    if (alliance?.leader_id !== playerId) {
      return new Response(JSON.stringify({ error: 'Only alliance leader can disband the alliance' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Remove all members from alliance
    await env.DB.prepare(
      'UPDATE players SET alliance_id = NULL WHERE alliance_id = ?'
    ).bind(playerData.alliance_id).run();

    // Delete alliance
    await env.DB.prepare(
      'DELETE FROM alliances WHERE alliance_id = ?'
    ).bind(playerData.alliance_id).run();

    // Delete from Alliance DO
    const allianceDOId = env.ALLIANCE_DO.idFromName(playerData.alliance_id);
    const allianceDO = env.ALLIANCE_DO.get(allianceDOId);

    const doRequest = new Request('http://alliance-do/disband', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ playerId })
    });

    await allianceDO.fetch(doRequest);

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Forward request to Alliance Durable Object
   */
  private static async forwardToAllianceDO(allianceId: string, request: Request, env: Env): Promise<Response> {
    const allianceDOId = env.ALLIANCE_DO.idFromName(allianceId);
    const allianceDO = env.ALLIANCE_DO.get(allianceDOId);
    return allianceDO.fetch(request);
  }

  /**
   * Verify JWT token and extract player ID
   */
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
