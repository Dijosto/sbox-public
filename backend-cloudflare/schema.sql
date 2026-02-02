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
INSERT INTO world_tiles (x, y, tile_type, level) VALUES
  (500, 500, 'npc_camp', 1),
  (501, 500, 'wilderness', 1),
  (502, 500, 'wilderness', 1),
  (500, 501, 'wilderness', 1),
  (501, 501, 'empty', 0);
