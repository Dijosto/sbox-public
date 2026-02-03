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

-- World tiles table
CREATE TABLE IF NOT EXISTS world_tiles (
  x INTEGER NOT NULL,
  y INTEGER NOT NULL,
  tile_type TEXT NOT NULL, -- 'city', 'npc_camp', 'wilderness', 'outpost', 'empty'
  owner_id TEXT, -- player_id if owned
  level INTEGER DEFAULT 1,
  resource_type TEXT, -- For wilderness tiles
  resource_bonus INTEGER DEFAULT 0,
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
  defender_id TEXT NOT NULL,
  timestamp INTEGER NOT NULL,
  winner TEXT NOT NULL, -- 'attacker' or 'defender'
  attacker_losses TEXT, -- JSON
  defender_losses TEXT, -- JSON
  loot TEXT, -- JSON
  combat_log TEXT, -- JSON
  FOREIGN KEY (attacker_id) REFERENCES players(player_id),
  FOREIGN KEY (defender_id) REFERENCES players(player_id)
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
  garrison TEXT NOT NULL, -- JSON: troop composition
  resources TEXT NOT NULL, -- JSON: available loot
  last_defeated INTEGER, -- Timestamp of last defeat
  respawn_time INTEGER, -- When camp regenerates
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

-- Sample data for testing
INSERT OR IGNORE INTO world_tiles (x, y, tile_type, level) VALUES
  (500, 500, 'npc_camp', 1),
  (520, 510, 'npc_camp', 2),
  (530, 520, 'npc_camp', 3),
  (501, 500, 'wilderness', 1),
  (502, 500, 'wilderness', 1),
  (500, 501, 'wilderness', 1),
  (501, 501, 'empty', 0);

-- Sample NPC camps
INSERT OR IGNORE INTO npc_camps (camp_id, x, y, camp_type, level, garrison, resources, max_attacks_per_day) VALUES
  ('npc_camp_500_500', 500, 500, 'goblin', 1, '{"goblin_warrior":{"quantity":20},"goblin_archer":{"quantity":15}}', '{"food":1000,"wood":800,"stone":500,"metal":300,"gold":100}', 5),
  ('npc_camp_520_510', 520, 510, 'barbarian', 2, '{"barbarian_raider":{"quantity":30},"barbarian_brute":{"quantity":20}}', '{"food":2500,"wood":2000,"stone":1500,"metal":800,"gold":300}', 3),
  ('npc_camp_530_520', 530, 520, 'dragon_lair', 3, '{"drake":{"quantity":5},"dragon_guard":{"quantity":15}}', '{"food":5000,"wood":4000,"stone":3000,"metal":2000,"gold":1000}', 1);
