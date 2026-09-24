# AGENTS.md

## Commands

```bash
dotnet build WHInventory.sln                      # build (IDE* warnings = errors → build fails on unused usings)
dotnet build WHInventory.sln -c Release           # release build
dotnet run --project Presentation/ASPNET/ASPNET.csproj  # dev server on http://localhost:8080
dotnet run --project Presentation/ASPNET/ASPNET.csproj --environment Development  # Swagger at /swagger (Dev only)
```

- Targets **.NET 9** (`net9.0`). There are **two** `.sln` files — build the root `WHInventory.sln`; `Presentation/ASPNET/ASPNET.sln` contains only the ASPNET project.
- No test projects. No EF migrations — schema is `EnsureCreated()` at startup. To reset: drop the DB and restart.
- Dev run expects a local PostgreSQL. The dev DB is the compose `db` service: `docker compose up -d db` → `appsettings.json` → `ConnectionStrings:DefaultConnection` already points at `localhost:5434` (matches compose's `5434:5432`). App binds `http://+:8080` (`appsettings.json` → `Kestrel`); 8080 is also the container port.

## Architecture

```
Core/Domain/          → entities, enums, BaseEntity (no dependencies)
Core/Application/     → MediatR CQRS + FluentValidation (depends on Domain)
Infrastructure/       → EF Core, Identity, JWT, SMTP, Serilog, seeding (implements Application)
Presentation/ASPNET   → controllers (REST API) + Razor Pages (Vue 3 + Syncfusion EJ2)
```

- **API routing**: `api/{controller}/{ActionName}` (RPC-style, not RESTful — controllers override `BaseApiController`'s `api/[controller]/[action]` with `api/[controller]`, action names come from `[HttpPost("CreateWarehouse")]` / `[HttpGet("GetWarehouseList")]` attributes)
- **Controllers** inherit `BaseApiController` → inject `ISender`, call `_sender.Send(request, ct)`, wrap in `ApiSuccessResult<T>`
- **MediatR pipeline** (in order): `LoggingBehaviour` → `ValidationBehaviour` (FluentValidation auto-run; throws `ValidationException`, caught by `GlobalApiExceptionHandlerMiddleware`)
- **Autowired audit**: `AuditFieldActionFilter` injects JWT `NameIdentifier` into `CreatedById`/`UpdatedById` fields before handlers run
- **Live notifications**: SignalR hub at `/hubs/notifications` (per-user groups), driven by the `NotificationManager` feature

## CQRS Pattern (one file = four classes)

Each `Commands/{Create,Update,Delete}*.cs` / `Queries/Get*.cs` co-locates:
1. `*Result` (DTO)
2. `*Request : IRequest<*Result>` (with init-only props)
3. `*Validator : AbstractValidator<*Request>`
4. `*Handler : IRequestHandler<*Request, *Result>`

**Must call** `SaveAsync()` after every write via `ICommandRepository<T>` + `IUnitOfWork`.
**Must call** `ApplyIsDeletedFilter(...)` on every read query to exclude soft-deleted rows (list queries pass `request.IsDeleted`, singles pass `false`; default param is `false`). Tenant filtering, by contrast, is automatic — see below.

## Dual DbContext

- **`CommandContext` (ICommandContext)** — writes via `ICommandRepository<T>`
- **`QueryContext` (IQueryContext)** — reads via direct LINQ on `DbSet<T>` (call `.AsNoTracking()`)

Provider: PostgreSQL (default) or SQL Server, set by `appsettings.json` → `DatabaseProvider`.

## Multi-tenancy (automatic, do not re-implement)

Every entity `BaseEntity` implements `IHasTenant`. EF global query filters enforce tenant scoping on every read **automatically** — do not add manual `TenantId` filters:
`IsRootScope || (CurrentTenantId != null && e.TenantId == CurrentTenantId)`. An unresolved request yields zero rows (fail-closed, no leakage).

- **New rows are stamped automatically**: `DataContext.SaveChangesAsync` calls `StampTenant()`. Don't set `TenantId` by hand.
- **Tenant resolution** (`TenantResolutionMiddleware`, runs after `UseAuthentication`): 1) JWT `TenantId` claim, 2) host subdomain slug (`acme.ustock.app` → tenant "acme"). Claim and host must agree or the request is 403. First label of an IP/apex host is never a slug; slug→tenant lookups are cached 5 min.
- **Root scope** (`tenantContext.SetRootScope()`) sees all tenants — reserved for platform-level code (e.g. `Tenant` registry, `TenantController`, public sign-up provisioning). Default root admin `admin@root.com`.
- **New tenant provisioning** (`TenantProvisioningService.ProvisionAsync`) temporarily switches the ambient tenant to run the system seeders (company, system warehouses, payment methods, admin user), then restores the caller's scope. Do not reorder this.

## Domain

- `BaseEntity`: **`Id` is a `string`** holding a timestamp-embedded sequential GUID (sortable), plus `TenantId`, `IsDeleted`, `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedById`, `UpdatedById`
- **Never hard-delete** — use entity `Delete()` method which sets `IsDeleted = true`
- **6 system warehouses** (`Customer`, `Vendor`, `Transfer`, `Adjustment`, `StockCount`, `Scrapping`) — `SystemWarehouse = true`. Tenant-scoped but seeded per-tenant; never delete or modify. Real warehouses are selected with `.Where(x => x.SystemWarehouse == false)`.
- `InventoryTransaction` is the central ledger; every movement creates child records linked by `ModuleName` (entity class name) + `ModuleId`
- `NumberSequenceService.GenerateNumber(entityName, prefix, suffix)` is the thread-safe path to human-readable document numbers (e.g. `SO`)

## Seeding

- System seed runs unconditionally at startup: default admin, roles, default tenant, company, system warehouses
- Demo seed runs only when `IsDemoVersion: true` (true in dev `appsettings.json`). ⚠️ No `appsettings.Production.json` exists and compose sets no override — a Production build from this tree still seeds demo data unless `IsDemoVersion=false` is supplied.
- Tenant creation is also a seeding concern (`TenantSeeder` + `TenantProvisioningService`)

## Security

- ASP.NET Identity + JWT Bearer. Default admin: `admin@root.com` / `123456` (configurable in `appsettings.json` → `AspNetIdentity:DefaultAdmin`)
- `RequireConfirmedEmail: true` by default — admin-created users bypass this; SMTP must be configured for self-registration
- `AllowPublicTenantSignUp` — when true, anyone can create an org at `/Accounts/SignUp` (true in `appsettings.json`; compose does not disable it for Production)
- `Npgsql.EnableLegacyTimestampBehavior = true` set in `Program.cs` line 5

## Frontend

- Razor Pages root: `/FrontEnd/Pages` (not `/Pages`)
- Each page has a paired `.cshtml.js` file — Vue 3 Composition API + Syncfusion EJ2 Grid/Charts + Bootstrap modals + SweetAlert2 + AJAX via `AxiosManager` (custom wrapper in `wwwroot/lib/indotalent/`)
- `SecurityManager.authorizePage(permissions)` and `validateToken()` called in every JS `setup()`
- **UI standard (binding): `docs/FRONTEND-UI-STANDARD.md`** — read §22 before editing markup/CSS, "done" is §24. Reuse `.card`/`.form-card*`/modals/SweetAlert2/Syncfusion instead of new variants; use only the app tokens (`--primary` `#1b84ff`, border `#dee2e6`, muted `#6c757d`, surface `#f8f9fa`, control border `#ced4da`, radius `.25rem`); Font Awesome 5 solid only (no emoji); page CSS goes in `wwwroot/css/*.css` via `@section styles`, never an inline `<style>` block or a block duplicated across pages.

## Adding a New Feature

1. Entity in `Core/Domain/Entities/` extending `BaseEntity` (tenant + soft-delete + audit come free)
2. `DbSet<T>` in `IEntityDbSet` + `DataContext` + `IEntityTypeConfiguration<T>` (registration needed in `DataContext.OnModelCreating`; tenant filter applies automatically via `IHasTenant`)
3. CQRS files in `Core/Application/Features/<FeatureManager>/`
4. Controller in `Presentation/ASPNET/BackEnd/Controllers/` inheriting `BaseApiController`
5. Razor Page + `.cshtml.js` in `Presentation/ASPNET/FrontEnd/Pages/<Feature>/`

## Code Style (enforced, will fail build)

- File-scoped namespaces (IDE0160/IDE0161)
- `_camelCase` for private / private-static-readonly fields
- `Async` suffix on all async methods
- Allman braces, `var` everywhere, `using` outside namespace
- Primary constructors forbidden (`csharp_style_prefer_primary_constructors = false:error`)
- Accessibility modifiers required on all non-interface members
- See `.editorconfig` for full ruleset

## Docker + Deploy

```bash
docker compose up -d             # full stack: db + app + Cloudflare tunnel
docker compose up -d db app      # skip tunnel
bash update.sh                   # VPS-only deploy: git pull + rebuild app image + health-check on :8080
```

- `.env` is gitignored; copy `.env.example`. The repo compose only consumes `DB_NAME`/`DB_USER`/`DB_PASSWORD`/`CLOUDFLARE_TUNNEL_TOKEN` — the `JWT_KEY`/`ADMIN_*`/`SMTP_*` entries in `.env.example` are not wired to compose or the app (the container runs on `appsettings.json` defaults unless overridden separately).
- Business dates use `appsettings.json` → `TimeZoneId` (`Asia/Dhaka`) — must match `TZ` env in compose; uploads live under `wwwroot/app_data/` (persisted volume in compose).