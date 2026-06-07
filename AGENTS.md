# AGENTS.md

## Commands

```bash
dotnet build WHInventory.sln                      # build (IDE* warnings = errors → build fails on unused usings)
dotnet run --project Presentation/ASPNET/ASPNET.csproj  # dev server on http://localhost:8080
dotnet run --project Presentation/ASPNET/ASPNET.csproj --environment Development  # Swagger at /swagger
```

No test projects exist. No EF migrations — schema is `EnsureCreated()` at startup. To reset: drop the DB and restart.

## Architecture

```
Core/Domain/          → entities, enums, BaseEntity (no dependencies)
Core/Application/     → MediatR CQRS + FluentValidation (depends on Domain)
Infrastructure/       → EF Core, Identity, JWT, SMTP, Serilog, seeding (implements Application)
Presentation/ASPNET   → controllers (REST API) + Razor Pages (Vue 3 + Syncfusion EJ2)
```

- **API routing**: `api/[controller]/[action]` (RPC-style, not RESTful — actions named like `CreateWarehouse`, `GetWarehouseList`)
- **Controllers** inherit `BaseApiController` → inject `ISender`, call `_sender.Send(request, ct)`, wrap in `ApiSuccessResult<T>`
- **Validation**: `ValidationBehaviour` MediatR pipeline runs FluentValidation automatically; errors thrown as `ValidationException`, caught by `GlobalApiExceptionHandlerMiddleware`
- **Autowired audit**: `AuditFieldActionFilter` injects JWT `NameIdentifier` into `CreatedById`/`UpdatedById` fields before handlers run

## CQRS Pattern (one file = four classes)

Each `Commands/{Create,Update,Delete}*.cs` / `Queries/Get*.cs` co-locates:
1. `*Result` (DTO)
2. `*Request : IRequest<*Result>` (with init-only props)
3. `*Validator : AbstractValidator<*Request>`
4. `*Handler : IRequestHandler<*Request, *Result>`

**Must call** `SaveAsync()` after every write via `ICommandRepository<T>` + `IUnitOfWork`.
**Must call** `ApplyIsDeletedFilter(false)` on every read query to exclude soft-deleted rows.

## Dual DbContext

- **`CommandContext` (ICommandContext)** — writes via `ICommandRepository<T>`
- **`QueryContext` (IQueryContext)** — reads via direct LINQ on `DbSet<T>` (call `.AsNoTracking()`)

Provider: PostgreSQL (default) or SQL Server, set by `appsettings.json` → `DatabaseProvider`.

## Domain

- All entities extend `BaseEntity` with `Id` (sequential GUID), `IsDeleted` (soft-delete flag), `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedById`, `UpdatedById`
- **Never hard-delete** — use entity `Delete()` method which sets `IsDeleted = true`
- **6 system warehouses** (`Customer`, `Vendor`, `Transfer`, `Adjustment`, `StockCount`, `Scrapping`) — `SystemWarehouse = true`, must never be deleted or modified
- `InventoryTransaction` is the central ledger; every movement creates child records linked by `ModuleName` (entity class name) + `ModuleId`

## Security

- ASP.NET Identity + JWT Bearer. Default admin: `admin@root.com` / `123456` (overridable in config)
- `RequireConfirmedEmail: true` by default — admin-created users bypass this; SMTP must be configured for self-registration
- `Npgsql.EnableLegacyTimestampBehavior = true` set in `Program.cs` line 5

## Frontend

- Razor Pages root: `/FrontEnd/Pages` (not `/Pages`)
- Each page has a paired `.cshtml.js` file — Vue 3 Composition API + Syncfusion EJ2 Grid/Charts + Bootstrap 5 modals + SweetAlert2 + custom AxiosManager
- `SecurityManager.authorizePage(permissions)` and `validateToken()` called in every JS `setup()`

## Adding a New Feature

1. Entity in `Core/Domain/Entities/` extending `BaseEntity`
2. `DbSet<T>` in `IEntityDbSet` + `DataContext` + `IEntityTypeConfiguration<T>`
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

## Docker

```bash
docker compose up -d             # full stack: db + app + Cloudflare tunnel
docker compose up -d db app      # skip tunnel
docker compose -f docker-compose.synology.yml --env-file .env up -d --build  # server deploy
```

`deploy.sh` is the production install script (Ubuntu 24.04 VPS). `.env` is gitignored — copy from `.env.example`.
