-- D1 Database Schema for Atlantis Strategy Game
-- Run with: wrangler d1 execute atlantis-strategy-db --file=./schema.sql

-- Players table
CREATE TABLE IF NOT EXISTS players (
  player_id TEXT PRIMARY KEY,
  steam_id TEXT UNIQUE NOT NULL,
  name TEXT NOT NULL,
  alliance_id TEXT,
  created_at INTEGER NOT NULL,
  last_login INTEGER,
  FOREIGN KEY (alliance_id) REFERENCES alliances(alliance_id)
);

CREATE INDEX IF NOT EXISTS idx_players_steam ON players(steam_id);
CREATE INDEX IF NOT EXISTS idx_players_name ON players(LOWER(name));
CREATE INDEX IF NOT EXISTS idx_players_alliance ON players(alliance_id);

-- Alliances table
CREATE TABLE IF NOT EXISTS alliances (
  alliance_id TEXT PRIMARY KEY,
  name TEXT UNIQUE NOT NULL,
  tag TEXT UNIQUE NOT NULL,
  leader_id TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  FOREIGN KEY (leader_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_alliances_name ON alliances(LOWER(name));
CREATE INDEX IF NOT EXISTS idx_alliances_tag ON alliances(LOWER(tag));

-- Alliance invitations and applications
CREATE TABLE IF NOT EXISTS alliance_invitations (
  invitation_id TEXT PRIMARY KEY,
  alliance_id TEXT NOT NULL,
  player_id TEXT NOT NULL,
  invited_by TEXT NOT NULL,
  type TEXT NOT NULL, -- 'invite' or 'application'
  status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'accepted', 'declined', 'expired'
  created_at INTEGER NOT NULL,
  expires_at INTEGER,
  FOREIGN KEY (alliance_id) REFERENCES alliances(alliance_id),
  FOREIGN KEY (player_id) REFERENCES players(player_id),
  FOREIGN KEY (invited_by) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_invitations_alliance ON alliance_invitations(alliance_id, status);
CREATE INDEX IF NOT EXISTS idx_invitations_player ON alliance_invitations(player_id, status);

-- Alliance diplomacy relationships
CREATE TABLE IF NOT EXISTS alliance_diplomacy (
  relationship_id TEXT PRIMARY KEY,
  alliance_id TEXT NOT NULL,
  target_alliance_id TEXT NOT NULL,
  relationship_type TEXT NOT NULL, -- 'ally', 'war', 'nap'
  established_at INTEGER NOT NULL,
  established_by TEXT NOT NULL, -- player_id who set the relationship
  FOREIGN KEY (alliance_id) REFERENCES alliances(alliance_id),
  FOREIGN KEY (target_alliance_id) REFERENCES alliances(alliance_id),
  FOREIGN KEY (established_by) REFERENCES players(player_id),
  UNIQUE(alliance_id, target_alliance_id)
);

CREATE INDEX IF NOT EXISTS idx_diplomacy_alliance ON alliance_diplomacy(alliance_id);
CREATE INDEX IF NOT EXISTS idx_diplomacy_type ON alliance_diplomacy(relationship_type);

-- World tiles table
-- World Map (750x750 grid that wraps around)
-- Wilderness types: 'forest' (lumber), 'savanna' (food), 'hills' (stone), 'mountains' (metal), 'plains' (outposts)
-- Bonuses scale from 5% at level 1 to 50% at level 10
CREATE TABLE IF NOT EXISTS world_tiles (
  x INTEGER NOT NULL,
  y INTEGER NOT NULL,
  tile_type TEXT NOT NULL, -- 'city', 'npc_camp', 'wilderness', 'outpost', 'empty'
  owner_id TEXT, -- player_id if owned
  level INTEGER DEFAULT 1,
  resource_type TEXT, -- 'forest', 'savanna', 'hills', 'mountains', 'plains'
  resource_bonus INTEGER DEFAULT 0, -- Production bonus: 5% (L1) to 50% (L10)
  npc_level INTEGER DEFAULT 0,
  region_x INTEGER GENERATED ALWAYS AS (x / 100) STORED,
  region_y INTEGER GENERATED ALWAYS AS (y / 100) STORED,
  PRIMARY KEY (x, y),
  FOREIGN KEY (owner_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_tiles_region ON world_tiles(region_x, region_y);
CREATE INDEX IF NOT EXISTS idx_tiles_owner ON world_tiles(owner_id);
CREATE INDEX IF NOT EXISTS idx_tiles_type ON world_tiles(tile_type);

-- Command log for audit trail
CREATE TABLE IF NOT EXISTS command_log (
  command_id TEXT PRIMARY KEY,
  player_id TEXT NOT NULL,
  timestamp INTEGER NOT NULL,
  command_type TEXT NOT NULL,
  payload TEXT, -- JSON
  result TEXT NOT NULL, -- 'success' or 'rejected'
  reason TEXT,
  state_hash TEXT,
  FOREIGN KEY (player_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_commands_player ON command_log(player_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_commands_timestamp ON command_log(timestamp DESC);

-- Battle reports
CREATE TABLE IF NOT EXISTS battle_reports (
  battle_id TEXT PRIMARY KEY,
  attacker_id TEXT NOT NULL,
  defender_id TEXT NOT NULL, -- Can be player_id or 'npc_<camp_id>' for NPC battles
  timestamp INTEGER NOT NULL,
  winner TEXT NOT NULL, -- 'attacker' or 'defender'
  attacker_losses TEXT, -- JSON
  defender_losses TEXT, -- JSON
  loot TEXT, -- JSON
  combat_log TEXT, -- JSON
  FOREIGN KEY (attacker_id) REFERENCES players(player_id)
  -- No FK on defender_id to allow NPC battles
);

CREATE INDEX IF NOT EXISTS idx_battles_attacker ON battle_reports(attacker_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_battles_defender ON battle_reports(defender_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_battles_timestamp ON battle_reports(timestamp DESC);

-- Messages/Mail system
CREATE TABLE IF NOT EXISTS messages (
  message_id TEXT PRIMARY KEY,
  recipient_id TEXT NOT NULL,
  sender_id TEXT, -- NULL for system messages
  sender_name TEXT,
  message_type TEXT NOT NULL, -- 'battle_report', 'system', 'alliance', 'player', 'scout_report'
  subject TEXT NOT NULL,
  body TEXT, -- Plain text or formatted message
  metadata TEXT, -- JSON data (battle_id for reports, etc.)
  is_read INTEGER DEFAULT 0,
  timestamp INTEGER NOT NULL,
  expires_at INTEGER, -- Auto-delete after this time
  FOREIGN KEY (recipient_id) REFERENCES players(player_id),
  FOREIGN KEY (sender_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_messages_recipient ON messages(recipient_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_messages_unread ON messages(recipient_id, is_read, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_messages_type ON messages(message_type, timestamp DESC);

-- NPC Camps (persistent world locations)
CREATE TABLE IF NOT EXISTS npc_camps (
  camp_id TEXT PRIMARY KEY,
  x INTEGER NOT NULL,
  y INTEGER NOT NULL,
  camp_type TEXT NOT NULL, -- 'goblin', 'barbarian', 'dragon_lair', 'ancient_ruins'
  level INTEGER NOT NULL,
  garrison TEXT NOT NULL, -- JSON: troop composition (full strength)
  resources TEXT NOT NULL, -- JSON: available loot
  last_defeated INTEGER, -- Timestamp of last defeat
  respawn_time INTEGER, -- When camp regenerates
  current_strength_percent INTEGER DEFAULT 100, -- Current garrison strength (0-100%)
  max_attacks_per_day INTEGER DEFAULT 3,
  attacks_today INTEGER DEFAULT 0,
  attacks_reset_at INTEGER,
  UNIQUE(x, y)
);

CREATE INDEX IF NOT EXISTS idx_npc_camps_location ON npc_camps(x, y);
CREATE INDEX IF NOT EXISTS idx_npc_camps_level ON npc_camps(level);
CREATE INDEX IF NOT EXISTS idx_npc_camps_respawn ON npc_camps(respawn_time);

-- State snapshots for replay/recovery
CREATE TABLE IF NOT EXISTS state_snapshots (
  snapshot_id TEXT PRIMARY KEY,
  player_id TEXT NOT NULL,
  timestamp INTEGER NOT NULL,
  state_json TEXT NOT NULL,
  state_hash TEXT NOT NULL,
  FOREIGN KEY (player_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_snapshots_player ON state_snapshots(player_id, timestamp DESC);

-- Leaderboards
CREATE TABLE IF NOT EXISTS leaderboard (
  player_id TEXT PRIMARY KEY,
  player_name TEXT NOT NULL,
  alliance_tag TEXT,
  rank INTEGER NOT NULL,
  score INTEGER NOT NULL,
  category TEXT NOT NULL, -- 'power', 'kills', 'resources', etc.
  updated_at INTEGER NOT NULL,
  FOREIGN KEY (player_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_leaderboard_category ON leaderboard(category, rank ASC);
CREATE INDEX IF NOT EXISTS idx_leaderboard_score ON leaderboard(category, score DESC);

-- Trade offers (marketplace system)
CREATE TABLE IF NOT EXISTS trade_offers (
  offer_id TEXT PRIMARY KEY,
  seller_id TEXT NOT NULL,
  resource_type TEXT NOT NULL, -- 'food', 'wood', 'stone', 'metal', 'gold'
  quantity INTEGER NOT NULL,
  price_per_unit REAL NOT NULL, -- gold per resource unit
  total_price INTEGER NOT NULL, -- quantity * price_per_unit (rounded)
  seller_fee INTEGER NOT NULL, -- gold fee paid by seller (equal to quantity)
  created_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL, -- created_at + 30 minutes (adjusted by speed multiplier)
  status TEXT NOT NULL DEFAULT 'active', -- 'active', 'sold', 'expired', 'cancelled'
  buyer_id TEXT, -- Set when sold
  completed_at INTEGER, -- Set when sold
  FOREIGN KEY (seller_id) REFERENCES players(player_id),
  FOREIGN KEY (buyer_id) REFERENCES players(player_id)
);

CREATE INDEX IF NOT EXISTS idx_trade_offers_active ON trade_offers(status, resource_type, price_per_unit ASC);
CREATE INDEX IF NOT EXISTS idx_trade_offers_seller ON trade_offers(seller_id, status);
CREATE INDEX IF NOT EXISTS idx_trade_offers_expiry ON trade_offers(expires_at, status);

-- Sample data for testing
INSERT OR IGNORE INTO world_tiles (x, y, tile_type, level, resource_type, resource_bonus) VALUES
  -- Anthropus Camps (various levels)
  (500, 500, 'npc_camp', 1, NULL, 0),
  (520, 510, 'npc_camp', 3, NULL, 0),
  (530, 520, 'npc_camp', 5, NULL, 0),
  (550, 530, 'npc_camp', 7, NULL, 0),
  (580, 550, 'npc_camp', 10, NULL, 0),
  -- Wilderness tiles (Dragons of Atlantis style)
  -- Forest tiles (lumber production)
  (501, 500, 'wilderness', 1, 'forest', 5),
  (502, 500, 'wilderness', 3, 'forest', 15),
  (503, 500, 'wilderness', 5, 'forest', 25),
  -- Savanna tiles (food production)
  (500, 501, 'wilderness', 1, 'savanna', 5),
  (500, 502, 'wilderness', 4, 'savanna', 20),
  (500, 503, 'wilderness', 7, 'savanna', 35),
  -- Hills tiles (stone production)
  (510, 510, 'wilderness', 2, 'hills', 10),
  (511, 510, 'wilderness', 6, 'hills', 30),
  -- Mountains tiles (metal production)
  (515, 515, 'wilderness', 3, 'mountains', 15),
  (516, 515, 'wilderness', 8, 'mountains', 40),
  -- Plains tiles (outpost placement only, no bonus)
  (520, 520, 'wilderness', 1, 'plains', 0),
  (521, 520, 'wilderness', 1, 'plains', 0),
  -- Empty tiles
  (501, 501, 'empty', 0, NULL, 0),
  (505, 505, 'empty', 0, NULL, 0);

-- Sample NPC camps (Anthropus Camps from Dragons of Atlantis)
INSERT OR IGNORE INTO npc_camps (camp_id, x, y, camp_type, level, garrison, resources, max_attacks_per_day) VALUES
  -- Level 1: Anthropus Camp (Beginner)
  ('npc_camp_500_500', 500, 500, 'anthropus', 1,
   '{"brat":{"quantity":1500},"cannibal":{"quantity":500},"stench":{"quantity":500},"she_devil":{"quantity":1000},"clubber":{"quantity":1000}}',
   '{"food":112500,"wood":5000,"stone":500,"metal":500,"gold":2500}', 10),

  -- Level 3: Anthropus Camp (Easy)
  ('npc_camp_520_510', 520, 510, 'anthropus', 3,
   '{"brat":{"quantity":7500},"cannibal":{"quantity":2500},"stench":{"quantity":2500},"she_devil":{"quantity":5000},"clubber":{"quantity":5000},"hurler":{"quantity":7500}}',
   '{"food":225000,"wood":10000,"stone":1000,"metal":1000,"gold":5000}', 8),

  -- Level 5: Anthropus Camp (Medium)
  ('npc_camp_530_520', 530, 520, 'anthropus', 5,
   '{"brat":{"quantity":30000},"cannibal":{"quantity":10000},"stench":{"quantity":10000},"she_devil":{"quantity":20000},"clubber":{"quantity":20000},"hurler":{"quantity":30000},"shredder":{"quantity":40000}}',
   '{"food":562500,"wood":25000,"stone":2500,"metal":2500,"gold":12500}', 5),

  -- Level 7: Anthropus Camp (Hard)
  ('npc_camp_550_530', 550, 530, 'anthropus', 7,
   '{"brat":{"quantity":150000},"cannibal":{"quantity":50000},"stench":{"quantity":30000},"she_devil":{"quantity":60000},"clubber":{"quantity":60000},"hurler":{"quantity":90000},"shredder":{"quantity":120000},"launcher":{"quantity":90000}}',
   '{"food":787500,"wood":35000,"stone":3500,"metal":3500,"gold":17500}', 3),

  -- Level 10: Anthropus Camp (Very Hard)
  ('npc_camp_580_550', 580, 550, 'anthropus', 10,
   '{"brat":{"quantity":750000},"cannibal":{"quantity":250000},"stench":{"quantity":120000},"she_devil":{"quantity":240000},"clubber":{"quantity":240000},"hurler":{"quantity":360000},"shredder":{"quantity":480000},"launcher":{"quantity":250000},"gnasher":{"quantity":300000}}',
   '{"food":1125000,"wood":50000,"stone":5000,"metal":5000,"gold":25000}', 1);
