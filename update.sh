#!/usr/bin/env bash
# Pull latest code, rebuild image, restart app.
# Run as root on the VPS: bash /opt/platform/apps/ustock/update.sh
set -euo pipefail

APP_DIR="/opt/platform/apps/ustock"

echo "▶ Pulling latest code…"
git -C "$APP_DIR" pull --autostash

echo "▶ Building image…"
docker build -t ustock-app:latest "$APP_DIR"

echo "▶ Restarting app…"
docker restart ustock-app

echo "▶ Waiting for app to respond…"
timeout 120 bash -c \
    'until curl -sf --max-time 5 http://localhost:8080 >/dev/null 2>&1; do sleep 4; done' \
    && echo "✓ App is up." \
    || { echo "✗ App did not respond in time. Check: docker logs --tail=50 ustock-app"; exit 1; }
