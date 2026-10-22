#!/usr/bin/env bash
#
# uStock restore: rebuilds the database and uploaded files from an archive produced by backup.sh.
#
# Usage:
#   scripts/restore.sh backups/ustock_20260101_020000.tar.gz
#
# DESTRUCTIVE: this DROPS and recreates the current database contents and OVERWRITES app_data.
# It requires an explicit confirmation. Never run it against production without a fresh backup of
# the current state first. Prefer restoring into an isolated/staging environment to verify.

set -euo pipefail

ARCHIVE="${1:-}"
if [[ -z "$ARCHIVE" || ! -f "$ARCHIVE" ]]; then
    echo "Usage: $0 <path-to-ustock_*.tar.gz>" >&2
    exit 1
fi

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DB_CONTAINER="${DB_CONTAINER:-ustock-db}"
APP_CONTAINER="${APP_CONTAINER:-ustock-app}"
APP_DATA_PATH="${APP_DATA_PATH:-/app/wwwroot/app_data}"

if [[ -f "$REPO_ROOT/.env" ]]; then
    # shellcheck disable=SC1091
    set -a; source "$REPO_ROOT/.env"; set +a
fi
: "${DB_NAME:?DB_NAME not set (expected in .env)}"
: "${DB_USER:?DB_USER not set (expected in .env)}"

echo "About to OVERWRITE database '$DB_NAME' and app_data from: $ARCHIVE"
read -r -p "Type 'RESTORE' to proceed: " CONFIRM
[[ "$CONFIRM" == "RESTORE" ]] || { echo "Aborted."; exit 1; }

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
tar -xzf "$ARCHIVE" -C "$WORK"

echo "[$(date -Is)] Stopping app so it does not write during restore..."
docker stop "$APP_CONTAINER" >/dev/null 2>&1 || true

echo "[$(date -Is)] Restoring database (pg_restore --clean)..."
# --clean drops existing objects first; --if-exists avoids errors on a fresh DB.
docker exec -i "$DB_CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --clean --if-exists --no-owner < "$WORK/db.dump"

if [[ -d "$WORK/app_data" ]]; then
    echo "[$(date -Is)] Restoring uploaded files..."
    docker start "$APP_CONTAINER" >/dev/null 2>&1 || true
    docker exec "$APP_CONTAINER" sh -c "rm -rf '$APP_DATA_PATH'/* '$APP_DATA_PATH'/.[!.]* 2>/dev/null || true"
    docker cp "$WORK/app_data/." "$APP_CONTAINER:$APP_DATA_PATH"
fi

echo "[$(date -Is)] Starting app..."
docker start "$APP_CONTAINER" >/dev/null 2>&1 || true

echo "[$(date -Is)] Restore complete. Verify: login, check product images, and reconcile stock totals."
