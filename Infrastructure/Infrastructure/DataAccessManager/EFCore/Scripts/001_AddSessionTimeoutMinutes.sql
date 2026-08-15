-- ---------------------------------------------------------------------------
-- Adds ApplicationUser.SessionTimeoutMinutes (per-user idle timeout).
--
-- Why this exists: the schema is created by EnsureCreated(), which only builds a
-- database that does not exist yet. It never alters one that does, so any
-- database created before this feature needs the column added by hand.
--
-- Run once per existing database. It is idempotent - safe to re-run.
--   PostgreSQL : psql -U <user> -d <database> -f 001_AddSessionTimeoutMinutes.sql
--
-- NULL means "use the system default" from appsettings.json
-- (AspNetIdentity:SessionTimeoutMinutes), so existing rows need no backfill.
-- ---------------------------------------------------------------------------

-- PostgreSQL (default provider)
ALTER TABLE auth."AspNetUsers"
    ADD COLUMN IF NOT EXISTS "SessionTimeoutMinutes" integer NULL;


-- ---------------------------------------------------------------------------
-- SQL Server equivalent (only if DatabaseProvider is set to "SqlServer").
-- Run this instead of the statement above.
-- ---------------------------------------------------------------------------
-- IF NOT EXISTS (
--     SELECT 1 FROM sys.columns
--     WHERE object_id = OBJECT_ID(N'[auth].[AspNetUsers]')
--       AND name = 'SessionTimeoutMinutes'
-- )
-- BEGIN
--     ALTER TABLE [auth].[AspNetUsers] ADD [SessionTimeoutMinutes] int NULL;
-- END
