# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build WHInventory.sln

# Run the application (entry point is the ASPNET project)
dotnet run --project Presentation/ASPNET/ASPNET.csproj

# Run with a specific environment
dotnet run --project Presentation/ASPNET/ASPNET.csproj --environment Development

# Build in Release mode
dotnet build WHInventory.sln -c Release

# Restore packages
dotnet restore WHInventory.sln
```

The app starts on `http://localhost:5001` (configured in `appsettings.json` under `Kestrel`). Port 5000 is permanently held by macOS AirPlay Receiver and must not be used. Swagger UI is available at `/swagger` in Development mode.

There are no migration commands — the database is created via `EnsureCreated()` at startup. To reset the database, drop it and restart.

## Architecture

This is a Clean Architecture solution with four projects:

```
Core/Domain          → Entities, enums, base classes (no dependencies)
Core/Application     → CQRS handlers, validators, service interfaces (depends on Domain)
Infrastructure/      → EF Core, ASP.NET Identity, JWT, email, file storage (implements Application interfaces)
Presentation/ASPNET  → ASP.NET Core host: REST API controllers + Razor Pages frontend
```

### CQRS and MediatR

All business operations use MediatR. Each feature in `Core/Application/Features/<FeatureManager>/` contains:
- `Commands/Create*.cs`, `Commands/Update*.cs`, `Commands/Delete*.cs` — each file defines a `*Request`, `*Result`, a FluentValidation `*Validator`, and a `*Handler` in the same file.
- `Queries/Get*.cs` — same co-located pattern.

Controllers dispatch directly to MediatR via `_sender.Send(request, cancellationToken)` and wrap the result in `ApiSuccessResult<T>`. Validation errors are thrown as `ValidationException` and caught by `GlobalApiExceptionHandlerMiddleware`.

Two MediatR pipeline behaviors run on every request (in order):
1. `LoggingBehaviour` — logs request/response
2. `ValidationBehaviour` — runs FluentValidation; throws on any failures

### Data Access: Dual DbContext

There are two separate EF Core contexts derived from a shared `DataContext`:
- `CommandContext` → registered as `ICommandContext`, used for writes via `ICommandRepository<T>`
- `QueryContext` → registered as `IQueryContext`, used for reads in Query handlers (direct LINQ against `DbSet<T>`)

`ICommandRepository<T>` provides Create/Update/Delete/Get. Persistence is explicit: always call `await _unitOfWork.SaveAsync(cancellationToken)` after mutations.

The database provider is selected at startup from `appsettings.json` `"DatabaseProvider"`: `"PostgreSQL"` (default) or `"SqlServer"`.

### Domain Model

All entities extend `BaseEntity` which provides:
- `Id` — sequential GUID (timestamp-embedded for sortability, generated in constructor)
- `IsDeleted` — soft-delete flag; never hard-delete via domain code, use `Delete()` which sets this flag
- `CreatedAtUtc`, `CreatedById`, `UpdatedAtUtc`, `UpdatedById` — audit fields

`ApplyIsDeletedFilter(false)` extension on `IQueryable<T>` must be called in all queries to exclude soft-deleted records.

### Inventory Transaction Model

`InventoryTransaction` is the central ledger. Every warehouse movement (delivery, goods receipt, returns, transfers, adjustments, scrapping, stock count) creates child `InventoryTransaction` records linked by `ModuleName` (the entity class name as string) and `ModuleId`.

`InventoryTransactionService` calculates `TransType` (In/Out), `Stock` (signed movement), and the virtual `WarehouseFrom`/`WarehouseTo` for each module type. Stock on hand = sum of confirmed `InventoryTransaction.Stock` per warehouse+product.

Six **system warehouses** are seeded automatically (name = system identifier, `SystemWarehouse = true`): `Customer`, `Vendor`, `Transfer`, `Adjustment`, `StockCount`, `Scrapping`. These serve as virtual counterparties in ledger entries and must not be deleted.

### Number Sequences

`NumberSequenceService.GenerateNumber(entityName, prefix, suffix)` generates human-readable document numbers (e.g., `"SO"` suffix for Sales Orders). It is thread-safe via a lock and auto-creates the sequence row on first use.

### Security

- ASP.NET Identity (`ApplicationUser : IdentityUser`) handles users and roles.
- JWT Bearer tokens are issued at login. `ExpireInMinute` and a `RefreshToken` flow are configured.
- `RequireConfirmedEmail: true` by default — SMTP must be configured for self-registration to work. Admin-created users bypass this.
- Default admin seeded: `admin@root.com` / `123456` (configurable in `appsettings.json` → `AspNetIdentity:DefaultAdmin`).

### Frontend

Razor Pages live in `Presentation/ASPNET/FrontEnd/Pages/`. Each page has a paired `.cshtml.js` file that handles client-side logic (API calls to the backend controllers). Pages are served from `/FrontEnd/Pages` as root (configured in `FrontEndConfiguration.cs`).

### Seeding

On every startup:
1. `EnsureCreated()` creates the schema if missing.
2. System seed runs unconditionally: admin user, roles, company record, system warehouses.
3. Demo seed runs only when `"IsDemoVersion": true` in `appsettings.json` — populates all entities with sample data.

### Adding a New Feature

Follow the existing pattern for any new domain entity:
1. Add entity to `Core/Domain/Entities/` extending `BaseEntity`.
2. Add `DbSet<T>` to `IEntityDbSet` and `DataContext`, and register an `IEntityTypeConfiguration<T>` in `DataContext.OnModelCreating`.
3. Add Commands and Queries under `Core/Application/Features/<NewFeatureManager>/` with co-located Request/Result/Validator/Handler.
4. Add a controller in `Presentation/ASPNET/BackEnd/Controllers/` inheriting `BaseApiController`.
5. Add Razor Page(s) in `Presentation/ASPNET/FrontEnd/Pages/<NewFeature>/`.
