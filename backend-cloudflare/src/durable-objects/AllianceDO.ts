import { DurableObject } from 'cloudflare:workers';

/**
 * Alliance Durable Object
 *
 * Manages alliance state including:
 * - Alliance metadata (name, tag, description)
 * - Member list with roles
 * - Alliance chat via WebSocket
 * - Diplomacy relationships
 * - Alliance statistics
 */

export interface AllianceMember {
  playerId: string;
  playerName: string;
  role: 'leader' | 'officer' | 'member';
  joinedAt: number;
  power: number; // Player power for rankings
}

export interface AllianceRelationship {
  targetAllianceId: string;
  targetAllianceName: string;
  type: 'ally' | 'war' | 'nap'; // Non-Aggression Pact
  establishedAt: number;
}

export interface AllianceChatMessage {
  messageId: string;
  playerId: string;
  playerName: string;
  message: string;
  timestamp: number;
}

export interface AllianceState {
  allianceId: string;
  name: string;
  tag: string; // Short 3-5 character tag (e.g., "DoA")
  description: string;
  leaderId: string;
  createdAt: number;
  members: AllianceMember[];
  relationships: AllianceRelationship[];
  chatHistory: AllianceChatMessage[];
  statistics: {
    totalPower: number;
    totalKills: number;
    totalWins: number;
    totalLosses: number;
  };
}

export class AllianceDurableObject extends DurableObject {
  private state: AllianceState | null = null;
  private sessions: Set<WebSocket> = new Set();

  constructor(ctx: DurableObjectState, env: any) {
    super(ctx, env);
  }

  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);
    const path = url.pathname;

    // WebSocket upgrade for alliance chat
    if (request.headers.get('Upgrade') === 'websocket') {
      return this.handleWebSocket(request);
    }

    // Load state if not already loaded
    if (!this.state) {
      await this.loadState();
    }

    // Route requests
    if (path.includes('/create')) {
      return this.handleCreate(request);
    } else if (path.includes('/info')) {
      return this.handleGetInfo(request);
    } else if (path.includes('/member/add')) {
      return this.handleAddMember(request);
    } else if (path.includes('/member/remove')) {
      return this.handleRemoveMember(request);
    } else if (path.includes('/member/promote')) {
      return this.handlePromoteMember(request);
    } else if (path.includes('/member/demote')) {
      return this.handleDemoteMember(request);
    } else if (path.includes('/chat/send')) {
      return this.handleSendChatMessage(request);
    } else if (path.includes('/chat/history')) {
      return this.handleGetChatHistory(request);
    } else if (path.includes('/diplomacy/set')) {
      return this.handleSetDiplomacy(request);
    } else if (path.includes('/disband')) {
      return this.handleDisband(request);
    }

    return new Response(JSON.stringify({ error: 'Unknown endpoint' }), {
      status: 404,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Load alliance state from Durable Object storage
   */
  private async loadState(): Promise<void> {
    const stored = await this.ctx.storage.get<AllianceState>('alliance');
    if (stored) {
      this.state = stored;
    }
  }

  /**
   * Save alliance state to Durable Object storage
   */
  private async saveState(): Promise<void> {
    if (this.state) {
      await this.ctx.storage.put('alliance', this.state);
    }
  }

  /**
   * Create a new alliance
   */
  private async handleCreate(request: Request): Promise<Response> {
    const body = await request.json() as {
      allianceId: string;
      name: string;
      tag: string;
      description: string;
      leaderId: string;
      leaderName: string;
    };

    // Initialize alliance state
    this.state = {
      allianceId: body.allianceId,
      name: body.name,
      tag: body.tag,
      description: body.description,
      leaderId: body.leaderId,
      createdAt: Date.now(),
      members: [
        {
          playerId: body.leaderId,
          playerName: body.leaderName,
          role: 'leader',
          joinedAt: Date.now(),
          power: 0
        }
      ],
      relationships: [],
      chatHistory: [],
      statistics: {
        totalPower: 0,
        totalKills: 0,
        totalWins: 0,
        totalLosses: 0
      }
    };

    await this.saveState();

    return new Response(JSON.stringify({ success: true, alliance: this.state }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Get alliance information
   */
  private async handleGetInfo(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    return new Response(JSON.stringify({ alliance: this.state }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Add a member to the alliance
   */
  private async handleAddMember(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      playerName: string;
      power: number;
    };

    // Check if member already exists
    if (this.state.members.find(m => m.playerId === body.playerId)) {
      return new Response(JSON.stringify({ error: 'Player already in alliance' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Add new member
    this.state.members.push({
      playerId: body.playerId,
      playerName: body.playerName,
      role: 'member',
      joinedAt: Date.now(),
      power: body.power
    });

    await this.saveState();

    // Broadcast to all connected members
    this.broadcastEvent({
      type: 'member_joined',
      data: {
        playerId: body.playerId,
        playerName: body.playerName
      }
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Remove a member from the alliance
   */
  private async handleRemoveMember(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      removedBy: string;
    };

    // Find the member to remove
    const memberIndex = this.state.members.findIndex(m => m.playerId === body.playerId);
    if (memberIndex === -1) {
      return new Response(JSON.stringify({ error: 'Member not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Check permissions (leader can remove anyone, officers can remove members)
    const remover = this.state.members.find(m => m.playerId === body.removedBy);
    const target = this.state.members[memberIndex];

    if (!remover) {
      return new Response(JSON.stringify({ error: 'Unauthorized' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Can't remove the leader
    if (target.role === 'leader') {
      return new Response(JSON.stringify({ error: 'Cannot remove alliance leader' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Officers can only remove members, leaders can remove anyone
    if (remover.role === 'officer' && target.role !== 'member') {
      return new Response(JSON.stringify({ error: 'Insufficient permissions' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Remove member
    const removedMember = this.state.members.splice(memberIndex, 1)[0];
    await this.saveState();

    // Broadcast event
    this.broadcastEvent({
      type: 'member_removed',
      data: {
        playerId: removedMember.playerId,
        playerName: removedMember.playerName
      }
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Promote a member to officer
   */
  private async handlePromoteMember(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      promotedBy: string;
    };

    // Only leader can promote
    const promoter = this.state.members.find(m => m.playerId === body.promotedBy);
    if (!promoter || promoter.role !== 'leader') {
      return new Response(JSON.stringify({ error: 'Only alliance leader can promote members' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const member = this.state.members.find(m => m.playerId === body.playerId);
    if (!member) {
      return new Response(JSON.stringify({ error: 'Member not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (member.role === 'officer') {
      return new Response(JSON.stringify({ error: 'Member is already an officer' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    member.role = 'officer';
    await this.saveState();

    this.broadcastEvent({
      type: 'member_promoted',
      data: {
        playerId: member.playerId,
        playerName: member.playerName,
        newRole: 'officer'
      }
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Demote an officer to member
   */
  private async handleDemoteMember(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      demotedBy: string;
    };

    // Only leader can demote
    const demoter = this.state.members.find(m => m.playerId === body.demotedBy);
    if (!demoter || demoter.role !== 'leader') {
      return new Response(JSON.stringify({ error: 'Only alliance leader can demote officers' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const member = this.state.members.find(m => m.playerId === body.playerId);
    if (!member) {
      return new Response(JSON.stringify({ error: 'Member not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (member.role !== 'officer') {
      return new Response(JSON.stringify({ error: 'Member is not an officer' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    member.role = 'member';
    await this.saveState();

    this.broadcastEvent({
      type: 'member_demoted',
      data: {
        playerId: member.playerId,
        playerName: member.playerName,
        newRole: 'member'
      }
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Send a chat message to the alliance
   */
  private async handleSendChatMessage(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      playerName: string;
      message: string;
    };

    // Verify sender is a member
    const member = this.state.members.find(m => m.playerId === body.playerId);
    if (!member) {
      return new Response(JSON.stringify({ error: 'Only alliance members can send messages' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const chatMessage: AllianceChatMessage = {
      messageId: crypto.randomUUID(),
      playerId: body.playerId,
      playerName: body.playerName,
      message: body.message,
      timestamp: Date.now()
    };

    // Add to chat history (keep last 100 messages)
    this.state.chatHistory.push(chatMessage);
    if (this.state.chatHistory.length > 100) {
      this.state.chatHistory.shift();
    }

    await this.saveState();

    // Broadcast to all connected members
    this.broadcastEvent({
      type: 'chat_message',
      data: chatMessage
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Get chat history
   */
  private async handleGetChatHistory(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    return new Response(JSON.stringify({ messages: this.state.chatHistory }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Set diplomacy relationship with another alliance
   */
  private async handleSetDiplomacy(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
      targetAllianceId: string;
      targetAllianceName: string;
      type: 'ally' | 'war' | 'nap' | 'neutral';
    };

    // Only leader and officers can manage diplomacy
    const member = this.state.members.find(m => m.playerId === body.playerId);
    if (!member || (member.role !== 'leader' && member.role !== 'officer')) {
      return new Response(JSON.stringify({ error: 'Insufficient permissions' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Remove existing relationship if it exists
    this.state.relationships = this.state.relationships.filter(
      r => r.targetAllianceId !== body.targetAllianceId
    );

    // Add new relationship (unless it's neutral, which means remove)
    if (body.type !== 'neutral') {
      this.state.relationships.push({
        targetAllianceId: body.targetAllianceId,
        targetAllianceName: body.targetAllianceName,
        type: body.type,
        establishedAt: Date.now()
      });
    }

    await this.saveState();

    this.broadcastEvent({
      type: 'diplomacy_changed',
      data: {
        targetAllianceId: body.targetAllianceId,
        targetAllianceName: body.targetAllianceName,
        type: body.type
      }
    });

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Disband the alliance
   */
  private async handleDisband(request: Request): Promise<Response> {
    if (!this.state) {
      return new Response(JSON.stringify({ error: 'Alliance not found' }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const body = await request.json() as {
      playerId: string;
    };

    // Only leader can disband
    if (body.playerId !== this.state.leaderId) {
      return new Response(JSON.stringify({ error: 'Only alliance leader can disband the alliance' }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Broadcast disbanding event
    this.broadcastEvent({
      type: 'alliance_disbanded',
      data: {}
    });

    // Clear all state
    await this.ctx.storage.deleteAll();
    this.state = null;

    // Close all WebSocket connections
    this.sessions.forEach(ws => ws.close(1000, 'Alliance disbanded'));
    this.sessions.clear();

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /**
   * Handle WebSocket upgrade for alliance chat
   */
  private handleWebSocket(request: Request): Response {
    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);

    this.ctx.acceptWebSocket(server);
    this.sessions.add(server);

    server.addEventListener('close', () => {
      this.sessions.delete(server);
    });

    return new Response(null, {
      status: 101,
      webSocket: client
    });
  }

  /**
   * Broadcast event to all connected WebSocket clients
   */
  private broadcastEvent(event: { type: string; data: any }): void {
    const message = JSON.stringify(event);
    this.sessions.forEach(session => {
      try {
        session.send(message);
      } catch (err) {
        // Client disconnected, remove from sessions
        this.sessions.delete(session);
      }
    });
  }
}
