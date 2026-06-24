#!/usr/bin/env bash
# Deploy latest code — run as root on the VPS: bash /opt/platform/apps/ustock/update.sh
set -euo pipefail

APP_DIR="/opt/platform/apps/ustock"
COMPOSE_DIR="/opt/platform/docker/apps/ustock"
COMPOSE_FILE="docker-compose.platform.yml"

echo "▶ Pulling latest code…"
git -C "$APP_DIR" pull --autostash

echo "▶ Syncing compose file…"
cp "$APP_DIR/$COMPOSE_FILE" "$COMPOSE_DIR/docker-compose.yml"

echo "▶ Rebuilding and restarting…"
cd "$COMPOSE_DIR"
docker compose --env-file .env up -d --build

echo "✓ Update complete."
