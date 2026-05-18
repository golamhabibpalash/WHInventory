#!/usr/bin/env bash
# Deploy latest code — run as root on the VPS: bash /opt/ustock/update.sh
set -euo pipefail

APP_DIR="/opt/ustock"
COMPOSE_FILE="docker-compose.synology.yml"

echo "▶ Pulling latest code…"
git -C "$APP_DIR" pull

echo "▶ Rebuilding and restarting…"
cd "$APP_DIR"
docker compose -f "$COMPOSE_FILE" --env-file .env up -d --build

echo "✓ Update complete."
