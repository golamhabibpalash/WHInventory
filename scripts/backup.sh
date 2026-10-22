#!/usr/bin/env bash
#
# uStock backup: dumps the PostgreSQL database and the uploaded files (app_data) into a single
# timestamped, compressed archive, then prunes archives older than the retention window.
#
# Run from anywhere; paths are resolved relative to the repo root (this script's parent).
# Intended to be driven by cron on the VPS, e.g. nightly:
#   0 2 * * *  /opt/ustock/scripts/backup.sh >> /var/log/ustock-backup.log 2>&1
#
# IMPORTANT: copy the resulting archive OFF this host (object storage / another machine). A backup
# that lives only on the server it protects does not survive that server being lost.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Configurable via environment; sensible defaults otherwise.
BACKUP_DIR="${BACKUP_DIR:-$REPO_ROOT/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
DB_CONTAINER="${DB_CONTAINER:-ustock-db}"
APP_CONTAINER="${APP_CONTAINER:-ustock-app}"
APP_DATA_PATH="${APP_DATA_PATH:-/app/wwwroot/app_data}"

# Load DB credentials from .env (same file compose uses).
if [[ -f "$REPO_ROOT/.env" ]]; then
    # shellcheck disable=SC1091
    set -a; source "$REPO_ROOT/.env"; set +a
fi
: "${DB_NAME:?DB_NAME not set (expected in .env)}"
: "${DB_USER:?DB_USER not set (expected in .env)}"

TS="$(date +%Y%m%d_%H%M%S)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

mkdir -p "$BACKUP_DIR"

echo "[$(date -Is)] Dumping database '$DB_NAME' from container '$DB_CONTAINER'..."
# Custom format (-F c) so restore can use pg_restore --clean and parallelism.
docker exec -t "$DB_CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" -F c > "$WORK/db.dump"

echo "[$(date -Is)] Copying uploaded files from '$APP_CONTAINER:$APP_DATA_PATH'..."
# app_data holds product images and documents referenced by DB rows; back them up together so a
# restore is internally consistent.
docker cp "$APP_CONTAINER:$APP_DATA_PATH" "$WORK/app_data" || {
    echo "[$(date -Is)] WARNING: could not copy app_data (container down?); archiving DB only." >&2
    mkdir -p "$WORK/app_data"
}

ARCHIVE="$BACKUP_DIR/ustock_${TS}.tar.gz"
tar -czf "$ARCHIVE" -C "$WORK" db.dump app_data
echo "[$(date -Is)] Wrote $ARCHIVE ($(du -h "$ARCHIVE" | cut -f1))"

echo "[$(date -Is)] Pruning archives older than ${RETENTION_DAYS} days..."
find "$BACKUP_DIR" -name 'ustock_*.tar.gz' -type f -mtime "+${RETENTION_DAYS}" -print -delete || true

echo "[$(date -Is)] Backup complete."
