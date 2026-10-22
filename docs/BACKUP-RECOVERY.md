# Backup & Recovery Runbook

uStock stores two kinds of persistent state that must be backed up **together**:

1. **PostgreSQL database** — all tenant data, inventory ledger, users, etc. (compose volume `pgdata`).
2. **Uploaded files** — product images and documents under `/app/wwwroot/app_data` (compose volume `app_data`). Database rows reference these files by name, so a DB restore without the matching files leaves broken image/document links.

There is **no managed backup** by default. The scripts below give a practical, zero-cost baseline that fits the existing Docker Compose deployment.

## What the scripts do

| Script | Purpose |
|---|---|
| `scripts/backup.sh` | `pg_dump` (custom format) + copy of `app_data`, bundled into one timestamped `backups/ustock_<ts>.tar.gz`; prunes archives older than `RETENTION_DAYS` (default 14). |
| `scripts/restore.sh` | Rebuilds the DB (`pg_restore --clean`) and `app_data` from one archive. Destructive; requires typing `RESTORE`. |

Both read `DB_NAME`/`DB_USER` from `.env` (the same file compose uses) and target containers `ustock-db` / `ustock-app`.

## Scheduling backups (cron on the VPS)

```bash
chmod +x scripts/backup.sh scripts/restore.sh
# Nightly at 02:00, logging to a file:
crontab -e
0 2 * * *  /opt/ustock/scripts/backup.sh >> /var/log/ustock-backup.log 2>&1
```

## Off-host copy (required)

A backup stored only on the server it protects does **not** survive losing that server. After each run, copy the archive elsewhere — pick whatever the budget allows:

```bash
# Examples — run after backup.sh, or append to it:
rclone copy backups/ remote:ustock-backups/        # any rclone target (S3/B2/Drive/...)
# or
scp backups/ustock_*.tar.gz user@another-host:/srv/ustock-backups/
```

Keep at least a few daily copies plus one weekly off-host. Protect the destination credentials; anyone with the archives has every tenant's data.

## Restoring

Prefer restoring into an **isolated** environment first to verify the archive, rather than straight over production.

```bash
# 1. Take a fresh backup of current state (so the restore itself is reversible).
scripts/backup.sh

# 2. Restore the chosen archive (stops the app, rebuilds DB + files, restarts).
scripts/restore.sh backups/ustock_20260101_020000.tar.gz
```

### Post-restore verification checklist

- App starts and login works.
- Product images and documents load (confirms `app_data` restored and matches DB rows).
- Spot-check a tenant: product list, a sales order, and its delivery/stock figures reconcile.
- Confirm tenant isolation is intact (each tenant sees only its own data).

## Disaster recovery (host lost)

1. Provision a new host with Docker + Docker Compose.
2. Clone the repo, create `.env` from `.env.example` with the **same** secrets as before (notably `JWT_KEY`; a different key invalidates all issued tokens — acceptable, users just re-login).
3. `docker compose up -d db app` (let `EnsureCreated` build an empty schema), then run `scripts/restore.sh <archive>`.
4. Work through the verification checklist above.

## Known limitations / future improvements

- `pg_dump` is a full logical dump; for large datasets consider WAL archiving / PITR (e.g. `wal-g`) for finer recovery points.
- Restore is all-or-nothing for the whole database; per-tenant restore would require selective export and is not provided.
- The scripts assume the compose container names; override via `DB_CONTAINER`/`APP_CONTAINER` env vars if changed.
