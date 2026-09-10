# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Targets **.NET 9** (`net9.0`). Entry point is the `ASPNET` project.

```bash
# Build the solution
dotnet build WHInventory.sln

# Run the application (dev server on http://localhost:8080)
dotnet run --project Presentation/ASPNET/ASPNET.csproj

# Run with a specific environment (Swagger at /swagger is Development-only)
dotnet run --project Presentation/ASPNET/ASPNET.csproj --environment Development

# Build in Release mode
dotnet build WHInventory.sln -c Release

# Restore packages
dotnet restore WHInventory.sln
```

The app binds to `http://+:8080` (`appsettings.json` → `Kestrel`).

**Build strictness (`Directory.Build.props`):** `EnforceCodeStyleInBuild=true` and `TreatWarningsAsErrors` scoped to `WarningsAsErrors=IDE*`. Code-style violations (unused usings, wrong namespace style, naming, etc.) **fail `dotnet build`**, not just warn. CA analyzers run as warnings only. `GenerateDocumentationFile=true` is set solely so IDE0005 (remove unnecessary usings) fires; `CS1591` is suppressed.

There are **no test projects** and **no EF migrations** — the schema is created by `EnsureCreated()` at startup. To reset the database, drop it and restart.

## Architecture

Clean Architecture, four projects:

```
Core/Domain          → Entities, enums, BaseEntity, marker interfaces (no dependencies)
Core/Application     → MediatR CQRS handlers, FluentValidation, service interfaces (depends on Domain)
Infrastructure/      → EF Core, ASP.NET Identity, JWT, SMTP, Serilog, seeding, tenant provisioning
Presentation/ASPNET  → ASP.NET Core host: REST-ish API controllers + Razor Pages frontend
```

### CQRS and MediatR

All business operations go through MediatR. Each feature in `Core/Application/Features/<FeatureManager>/`:
- `Commands/{Create,Update,Delete}*.cs` and `Queries/Get*.cs` — **one file defines four classes**: `*Result` (DTO), `*Request : IRequest<*Result>` (init-only props), `*Validator : AbstractValidator<*Request>`, `*Handler : IRequestHandler<*Request,*Result>`.

Controllers inherit `BaseApiController`, inject `ISender`, call `_sender.Send(request, ct)`, and wrap the result in `ApiSuccessResult<T>`. `ValidationException` is thrown by the pipeline and caught by `GlobalApiExceptionHandlerMiddleware`.

**API routing is RPC-style, not RESTful:** `api/[controller]/[action]` with action names like `CreateWarehouse`, `GetWarehouseList`.

MediatR pipeline behaviors run on every request, in order:
1. `LoggingBehaviour` — logs request/response
2. `ValidationBehaviour` — runs FluentValidation; throws on any failure

`AuditFieldActionFilter` (MVC action filter) injects the JWT `NameIdentifier` into `CreatedById` / `UpdatedById` before handlers run — do not set audit-user fields by hand.

### Data Access: Dual DbContext

Two EF Core contexts derived from a shared `DataContext`:
- `CommandContext` → `ICommandContext`, writes via `ICommandRepository<T>` (Create/Update/Delete/Get)
- `QueryContext` → `IQueryContext`, reads via direct LINQ on `DbSet<T>` (use `.AsNoTracking()`)

Persistence is explicit: always `await _unitOfWork.SaveAsync(cancellationToken)` after mutations.

Every read query must call the `ApplyIsDeletedFilter(false)` extension on `IQueryable<T>` to exclude soft-deleted rows. (Tenant filtering, by contrast, is automatic — see below.)

Provider is chosen at startup from `appsettings.json` → `"DatabaseProvider"`: `"PostgreSQL"` (default) or `"SqlServer"`. `Npgsql.EnableLegacyTimestampBehavior = true` is set early in `Program.cs`.

### Multi-tenancy (automatic — do not re-implement)

`BaseEntity` implements `IHasTenant` (`string? TenantId`). EF Core **global query filters** enforce tenant scoping on every read automatically:
`IsRootScope || (CurrentTenantId != null && e.TenantId == CurrentTenantId)`. An unresolved tenant yields **zero rows** (fail-closed, no cross-tenant leakage). **Never add manual `TenantId` filters in handlers.**

- **Row stamping is automatic**: `DataContext.SaveChangesAsync` calls `StampTenant()`. Do not set `TenantId` by hand.
- **Tenant resolution** (`TenantResolutionMiddleware`, after `UseAuthentication`): (1) JWT `TenantId` claim, (2) host subdomain slug (`acme.ustock.app` → tenant `acme`). Claim and host must agree or the request is 403. The first label of an IP/apex host is never treated as a slug; slug→tenant lookups are cached ~5 min.
- **Root scope** (`ITenantContext.SetRootScope()`) sees all tenants — reserved for platform code (`Tenant` registry / `TenantController`, public sign-up provisioning). Default root admin is `admin@root.com`.
- **New-tenant provisioning** (`TenantProvisioningService.ProvisionAsync`) temporarily switches the ambient tenant to run the system seeders (company, system warehouses, payment methods, admin user), then restores the caller's scope. Do not reorder this.

Design notes: row-level `TenantId`, one-user-one-tenant, subdomain + JWT; enforcement lives only in EF query filters.

### Domain Model

`BaseEntity` provides:
- `Id` — a **`string`** holding a timestamp-embedded sequential GUID (sortable), generated in the constructor
- `TenantId` — see multi-tenancy above
- `IsDeleted` — soft-delete flag; never hard-delete, call the entity's `Delete()` method
- `CreatedAtUtc`, `CreatedById`, `UpdatedAtUtc`, `UpdatedById` — audit fields

### Inventory Transaction Model

`InventoryTransaction` is the central ledger. Every warehouse movement (delivery, goods receipt, returns, transfers, adjustments, scrapping, stock count) creates child `InventoryTransaction` records linked by `ModuleName` (the entity class name as a string) and `ModuleId`.

`InventoryTransactionService` calculates `TransType` (In/Out), `Stock` (signed movement), and the virtual `WarehouseFrom`/`WarehouseTo` per module type. Stock on hand = sum of confirmed `InventoryTransaction.Stock` per warehouse+product.

**Six system warehouses** (`Customer`, `Vendor`, `Transfer`, `Adjustment`, `StockCount`, `Scrapping`; `SystemWarehouse = true`) are seeded **per tenant** and act as virtual counterparties. Never delete or modify them. Real warehouses are selected with `.Where(x => x.SystemWarehouse == false)`.

### Number Sequences

`NumberSequenceService.GenerateNumber(entityName, prefix, suffix)` generates human-readable document numbers (e.g. `"SO"` suffix for sales orders). Thread-safe via a lock; auto-creates the sequence row on first use.

### Security

- ASP.NET Identity (`ApplicationUser : IdentityUser`) for users and roles.
- JWT Bearer tokens issued at login; `ExpireInMinute` and a `RefreshToken` flow are configured. The JWT also carries the `TenantId` claim.
- `RequireConfirmedEmail: true` by default — SMTP must be configured for self-registration; admin-created users bypass it.
- Default admin seeded: `admin@root.com` / `123456` (configurable in `appsettings.json` → `AspNetIdentity:DefaultAdmin`).
- `AllowPublicTenantSignUp` — when true, any visitor can create an organisation at `/Accounts/SignUp` (off by default in the production compose file).

### Frontend

Razor Pages live in `Presentation/ASPNET/FrontEnd/Pages/` and are served with `/FrontEnd/Pages` as root (`FrontEndConfiguration.cs`). Each page has a paired `.cshtml.js` file: **Vue 3 Composition API** + **Syncfusion EJ2** (Grid/Charts) + Bootstrap 5 modals + SweetAlert2, with API calls through `AxiosManager` (custom wrapper in `wwwroot/lib/indotalent/`). Every JS `setup()` calls `SecurityManager.authorizePage(permissions)` and `validateToken()`.

Dates are displayed as `DD/MM/YYYY` throughout.

**UI standard (binding): [`docs/FRONTEND-UI-STANDARD.md`](docs/FRONTEND-UI-STANDARD.md).** Read Section 22 before editing markup/CSS; a task meets "done" only per Section 24. Key rules for this codebase:
- **Reuse components** — Bootstrap `.card`/`.card-header`/`.card-body` (re-styled in `Shared/AdminLTE/__css.cshtml`), `.form-card*`, Bootstrap modals, SweetAlert2, Syncfusion controls. Do not hand-roll a new variant of an existing pattern.
- **Tokens only** — `--primary` (`#1b84ff`), border `#dee2e6`, muted `#6c757d`, subtle surface `#f8f9fa`, control border `#ced4da`, radius `.25rem`. No arbitrary hex, spacing, or radius values in components.
- **Icons** — Font Awesome 5 solid (`fas fa-*`) only. No emoji, no mixing icon sets.
- **Page CSS** — shared rules go in a stylesheet under `wwwroot/css/` pulled in via `@section styles { <link ... asp-append-version="true" /> }`. Never inline a `<style>` block in `.cshtml`, never duplicate one across pages.

### Seeding

On every startup:
1. `EnsureCreated()` creates the schema if missing.
2. System seed runs unconditionally: default admin + roles, default tenant, company record, system warehouses (and per-tenant seeds run through the provisioning service).
3. Demo seed runs only when `"IsDemoVersion": true` in `appsettings.json` — populates all entities with sample data.

### Adding a New Feature

1. Add the entity to `Core/Domain/Entities/` extending `BaseEntity` (tenant + soft-delete + audit come free).
2. Add `DbSet<T>` to `IEntityDbSet` and `DataContext`, and register an `IEntityTypeConfiguration<T>` in `DataContext.OnModelCreating` (the tenant filter applies automatically via `IHasTenant`).
3. Add Commands/Queries under `Core/Application/Features/<NewFeatureManager>/` with the co-located Request/Result/Validator/Handler.
4. Add a controller in `Presentation/ASPNET/BackEnd/Controllers/` inheriting `BaseApiController`.
5. Add Razor Page(s) + `.cshtml.js` in `Presentation/ASPNET/FrontEnd/Pages/<NewFeature>/`.

## Code Style (enforced — violations fail the build)

- File-scoped namespaces (IDE0160/IDE0161).
- `_camelCase` for private and private-static-readonly fields.
- `Async` suffix required on all async methods.
- `var` everywhere (built-in types included); Allman braces; `using` directives outside the namespace.
- Primary constructors are forbidden (`csharp_style_prefer_primary_constructors = false:error`).
- Accessibility modifiers required on all non-interface members.
- Full ruleset in `.editorconfig`.

## Docker & Deploy

```bash
docker compose up -d          # full stack: PostgreSQL + app + Cloudflare tunnel
docker compose up -d db app   # app + database only (skip the tunnel)
```

- `.env` is gitignored — copy `.env.example` and adjust (DB creds, JWT key, admin, SMTP).
