#!/bin/bash

# World Map Setup Script
# Generates the full 750x750 world map and loads it into D1 database

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}World Map Setup${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Generate world map SQL
echo -e "${YELLOW}[1/4] Generating world map data...${NC}"
echo -e "${BLUE}This will create ~20,000 Anthropus camps and 100,000 wilderness tiles${NC}"

npx tsx scripts/generate-world.ts > generated-world.sql

# Check file size
FILE_SIZE=$(wc -c < generated-world.sql)
LINES=$(wc -l < generated-world.sql)

echo -e "${GREEN}Generated SQL file: $(($FILE_SIZE / 1024 / 1024))MB, $LINES lines${NC}"
echo ""

# Step 2: Initialize base schema
echo -e "${YELLOW}[2/4] Initializing database schema...${NC}"
wrangler d1 execute atlantis-strategy-db --local --file=./schema.sql
echo ""

# Step 3: Load world data
echo -e "${YELLOW}[3/4] Loading world map into database (this may take a minute)...${NC}"
wrangler d1 execute atlantis-strategy-db --local --file=./generated-world.sql
echo ""

# Step 4: Verify data
echo -e "${YELLOW}[4/4] Verifying world map data...${NC}"

echo "Counting NPC camps by level:"
wrangler d1 execute atlantis-strategy-db --local --command="SELECT level, COUNT(*) as count FROM npc_camps GROUP BY level ORDER BY level"
echo ""

echo "Counting world tiles by type:"
wrangler d1 execute atlantis-strategy-db --local --command="SELECT tile_type, COUNT(*) as count FROM world_tiles GROUP BY tile_type"
echo ""

echo "Counting wilderness by resource type:"
wrangler d1 execute atlantis-strategy-db --local --command="SELECT resource_type, COUNT(*) as count FROM world_tiles WHERE tile_type='wilderness' GROUP BY resource_type"
echo ""

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}World Map Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "  1. Start dev server: ${YELLOW}npm run dev${NC}"
echo "  2. Run tests: ${YELLOW}./test-world-pvp.sh${NC}"
