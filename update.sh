#!/usr/bin/env bash
# Pull latest code, rebuild image, recreate container.
# Run as root on the VPS: bash /opt/platform/apps/ustock/update.sh
set -euo pipefail

APP_DIR="/opt/platform/apps/ustock"
COMPOSE_DIR="/opt/platform/docker/apps/ustock"

echo "▶ Pulling latest code…"
git -C "$APP_DIR" pull --autostash

echo "▶ Rebuilding and restarting…"
cd "$COMPOSE_DIR"
docker compose up -d --build app

echo "▶ Waiting for app to respond…"
timeout 120 bash -c \
    'until curl -sf --max-time 5 http://localhost:8080 >/dev/null 2>&1; do sleep 4; done' \
    && echo "✓ App is up." \
    || { echo "✗ App did not respond in time. Check: docker logs --tail=50 ustock-app"; exit 1; }
