using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Infrastructure.DataAccessManager.EFCore;



public static class DI
{
    public static IServiceCollection RegisterDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseProvider = configuration["DatabaseProvider"];

        // Register Context
        switch (databaseProvider)
        {
            //case "MySql":
            //    services.AddDbContext<DataContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<CommandContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<QueryContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    break;

            case "PostgreSQL":
                services.AddDbContext<DataContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;

            case "SqlServer":
            default:
                services.AddDbContext<DataContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;
        }


        services.AddHttpContextAccessor();
        services.AddScoped<ICommandContext, CommandContext>();
        services.AddScoped<IQueryContext, QueryContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));


        return services;
    }

    public static IHost CreateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dataContext = serviceProvider.GetRequiredService<DataContext>();

        if (dataContext.Database.IsNpgsql())
            MigratePublicSchemaToNamedSchemas(dataContext);

        dataContext.Database.EnsureCreated();

        EnsureMissingTables(dataContext);

        return host;
    }

    // Runs before EnsureCreated so databases created before the core/auth schema split
    // are transparently moved without requiring a manual drop.
    private static void MigratePublicSchemaToNamedSchemas(DataContext dataContext)
    {
        dataContext.Database.ExecuteSqlRaw(@"
            CREATE SCHEMA IF NOT EXISTS core;
            CREATE SCHEMA IF NOT EXISTS auth;
        ");

        dataContext.Database.ExecuteSqlRaw(@"
            DO $$
            DECLARE t TEXT;
            BEGIN
                FOREACH t IN ARRAY ARRAY[
                    'AspNetUsers','AspNetRoles','AspNetUserRoles',
                    'AspNetUserClaims','AspNetRoleClaims','AspNetUserLogins','AspNetUserTokens'
                ] LOOP
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = t
                    ) THEN
                        EXECUTE format('ALTER TABLE public.%I SET SCHEMA auth', t);
                    END IF;
                END LOOP;
            END $$;
        ");

        dataContext.Database.ExecuteSqlRaw(@"
            DO $$
            DECLARE t TEXT;
            BEGIN
                FOREACH t IN ARRAY ARRAY[
                    'Token','Todo','TodoItem','Company','FileImage','FileDocument',
                    'NumberSequence','CustomerGroup','CustomerCategory','VendorGroup',
                    'VendorCategory','Warehouse','Customer','Vendor','UnitMeasure',
                    'ProductGroup','Brand','Product','CustomerContact','VendorContact',
                    'Tax','SalesOrder','SalesOrderItem','PurchaseOrder','PurchaseOrderItem',
                    'InventoryTransaction','DeliveryOrder','GoodsReceive','SalesReturn',
                    'PurchaseReturn','TransferIn','TransferOut','StockCount',
                    'NegativeAdjustment','PositiveAdjustment','Scrapping',
                    'NavigationMenuSortOrder','AuditLog','UserActivityLog'
                ] LOOP
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = t
                    ) THEN
                        EXECUTE format('ALTER TABLE public.%I SET SCHEMA core', t);
                    END IF;
                END LOOP;
            END $$;
        ");
    }

    private static void EnsureMissingTables(DataContext dataContext)
    {
        var isPostgres = dataContext.Database.IsNpgsql();

        if (isPostgres)
        {
            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Company"" ADD COLUMN IF NOT EXISTS ""LogoName"" varchar(500) NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Product"" ADD COLUMN IF NOT EXISTS ""ImageName"" varchar(255) NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Product"" ADD COLUMN IF NOT EXISTS ""IsWarrantyApplicable"" boolean NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Product"" ADD COLUMN IF NOT EXISTS ""Barcode"" varchar(100) NULL;
                CREATE INDEX IF NOT EXISTS ""IX_Product_Barcode""
                    ON core.""Product"" (""Barcode"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""ProductGroup"" ADD COLUMN IF NOT EXISTS ""ParentId"" varchar(50) NULL;
                CREATE INDEX IF NOT EXISTS ""IX_ProductGroup_ParentId""
                    ON core.""ProductGroup"" (""ParentId"");
                ALTER TABLE core.""ProductGroup"" DROP CONSTRAINT IF EXISTS ""FK_ProductGroup_ProductGroup_ParentId"";
                ALTER TABLE core.""ProductGroup"" ADD CONSTRAINT ""FK_ProductGroup_ProductGroup_ParentId""
                    FOREIGN KEY (""ParentId"") REFERENCES core.""ProductGroup"" (""Id"") ON DELETE RESTRICT;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""Brand"" (
                    ""Id""            varchar(50)   NOT NULL PRIMARY KEY,
                    ""Name""          varchar(255)  NULL,
                    ""Number""        varchar(50)   NULL,
                    ""Description""   varchar(4000) NULL,
                    ""ImageName""     varchar(255)  NULL,
                    ""Status""        varchar(50)   NULL,
                    ""IsDeleted""     boolean       NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""  timestamp     NULL,
                    ""CreatedById""   varchar(450)  NULL,
                    ""UpdatedAtUtc""  timestamp     NULL,
                    ""UpdatedById""   varchar(450)  NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_Brand_IsDeleted"" ON core.""Brand"" (""IsDeleted"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Brand_Name"" ON core.""Brand"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_Brand_Number"" ON core.""Brand"" (""Number"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Product"" ADD COLUMN IF NOT EXISTS ""BrandId"" varchar(50) NULL;
                CREATE INDEX IF NOT EXISTS ""IX_Product_BrandId""
                    ON core.""Product"" (""BrandId"");
                ALTER TABLE core.""Product"" DROP CONSTRAINT IF EXISTS ""FK_Product_Brand_BrandId"";
                ALTER TABLE core.""Product"" ADD CONSTRAINT ""FK_Product_Brand_BrandId""
                    FOREIGN KEY (""BrandId"") REFERENCES core.""Brand"" (""Id"") ON DELETE SET NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""NavigationMenuSortOrder"" (
                    ""Id""            varchar(50)  NOT NULL PRIMARY KEY,
                    ""IsDeleted""     boolean      NOT NULL DEFAULT false,
                    ""CreatedAtUtc""  timestamp    NULL,
                    ""CreatedById""   varchar(450) NULL,
                    ""UpdatedAtUtc""  timestamp    NULL,
                    ""UpdatedById""   varchar(450) NULL,
                    ""UserId""        varchar(450) NULL,
                    ""SortOrderJson"" text         NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_NavigationMenuSortOrder_UserId""
                    ON core.""NavigationMenuSortOrder"" (""UserId"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""AuditLog"" (
                    ""Id""            varchar(50)   NOT NULL PRIMARY KEY,
                    ""EntityType""    varchar(255)  NULL,
                    ""EntityId""      varchar(50)   NULL,
                    ""OperationType"" varchar(50)   NULL,
                    ""OldValues""     text          NULL,
                    ""NewValues""     text          NULL,
                    ""UserId""        varchar(450)  NULL,
                    ""IpAddress""     varchar(50)   NULL,
                    ""CreatedAtUtc""  timestamp     NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_EntityType"" ON core.""AuditLog"" (""EntityType"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_UserId""     ON core.""AuditLog"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_CreatedAtUtc"" ON core.""AuditLog"" (""CreatedAtUtc"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""UserActivityLog"" (
                    ""Id""           varchar(50)   NOT NULL PRIMARY KEY,
                    ""UserId""       varchar(450)  NULL,
                    ""UserEmail""    varchar(255)  NULL,
                    ""ActivityType"" varchar(100)  NULL,
                    ""Description""  varchar(2000) NULL,
                    ""PageUrl""      varchar(2000) NULL,
                    ""IpAddress""    varchar(50)   NULL,
                    ""UserAgent""    varchar(500)  NULL,
                    ""CreatedAtUtc"" timestamp     NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_UserId""       ON core.""UserActivityLog"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_ActivityType""  ON core.""UserActivityLog"" (""ActivityType"");
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_CreatedAtUtc""  ON core.""UserActivityLog"" (""CreatedAtUtc"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""Product"" ADD COLUMN IF NOT EXISTS ""WarrantyDays"" int NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""PricePolicy"" (
                    ""Id""            varchar(50)   NOT NULL PRIMARY KEY,
                    ""Name""          varchar(255)  NULL,
                    ""Code""          varchar(50)   NULL,
                    ""Description""   varchar(4000) NULL,
                    ""Priority""      int           NOT NULL DEFAULT 0,
                    ""IsActive""      boolean       NOT NULL DEFAULT TRUE,
                    ""EffectiveFrom"" timestamp     NULL,
                    ""EffectiveTo""   timestamp     NULL,
                    ""IsDeleted""     boolean       NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""  timestamp     NULL,
                    ""CreatedById""   varchar(450)  NULL,
                    ""UpdatedAtUtc""  timestamp     NULL,
                    ""UpdatedById""   varchar(450)  NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_PricePolicy_IsDeleted"" ON core.""PricePolicy"" (""IsDeleted"");
                CREATE INDEX IF NOT EXISTS ""IX_PricePolicy_Name""      ON core.""PricePolicy"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_PricePolicy_Code""      ON core.""PricePolicy"" (""Code"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""ProductPrice"" (
                    ""Id""                     varchar(50)  NOT NULL PRIMARY KEY,
                    ""ProductId""              varchar(50)  NULL,
                    ""PricePolicyId""          varchar(50)  NULL,
                    ""CalculationMethod""      int          NOT NULL DEFAULT 0,
                    ""FixedPrice""             float8       NULL,
                    ""MarkupPercent""          float8       NULL,
                    ""MarkupAmount""           float8       NULL,
                    ""MarginPercent""          float8       NULL,
                    ""FormulaMultiplier""      float8       NULL,
                    ""MinimumSellingPrice""    float8       NULL,
                    ""MaximumDiscountPercent"" float8       NULL,
                    ""EffectiveFrom""          timestamp    NULL,
                    ""EffectiveTo""            timestamp    NULL,
                    ""Priority""               int          NOT NULL DEFAULT 0,
                    ""IsActive""               boolean      NOT NULL DEFAULT TRUE,
                    ""IsDeleted""              boolean      NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""           timestamp    NULL,
                    ""CreatedById""            varchar(450) NULL,
                    ""UpdatedAtUtc""           timestamp    NULL,
                    ""UpdatedById""            varchar(450) NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_ProductPrice_IsDeleted""    ON core.""ProductPrice"" (""IsDeleted"");
                CREATE INDEX IF NOT EXISTS ""IX_ProductPrice_ProductId""    ON core.""ProductPrice"" (""ProductId"");
                CREATE INDEX IF NOT EXISTS ""IX_ProductPrice_PricePolicyId"" ON core.""ProductPrice"" (""PricePolicyId"");
                ALTER TABLE core.""ProductPrice""
                    DROP CONSTRAINT IF EXISTS ""FK_ProductPrice_Product_ProductId"";
                ALTER TABLE core.""ProductPrice""
                    ADD CONSTRAINT ""FK_ProductPrice_Product_ProductId""
                    FOREIGN KEY (""ProductId"") REFERENCES core.""Product"" (""Id"") ON DELETE SET NULL;
                ALTER TABLE core.""ProductPrice""
                    DROP CONSTRAINT IF EXISTS ""FK_ProductPrice_PricePolicy_PricePolicyId"";
                ALTER TABLE core.""ProductPrice""
                    ADD CONSTRAINT ""FK_ProductPrice_PricePolicy_PricePolicyId""
                    FOREIGN KEY (""PricePolicyId"") REFERENCES core.""PricePolicy"" (""Id"") ON DELETE SET NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""QuantityBreak"" (
                    ""Id""             varchar(50)  NOT NULL PRIMARY KEY,
                    ""ProductPriceId"" varchar(50)  NULL,
                    ""MinQuantity""    float8       NOT NULL DEFAULT 1,
                    ""MaxQuantity""    float8       NULL,
                    ""Price""          float8       NOT NULL DEFAULT 0,
                    ""IsDeleted""      boolean      NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""   timestamp    NULL,
                    ""CreatedById""    varchar(450) NULL,
                    ""UpdatedAtUtc""   timestamp    NULL,
                    ""UpdatedById""    varchar(450) NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_QuantityBreak_IsDeleted""    ON core.""QuantityBreak"" (""IsDeleted"");
                CREATE INDEX IF NOT EXISTS ""IX_QuantityBreak_ProductPriceId"" ON core.""QuantityBreak"" (""ProductPriceId"");
                ALTER TABLE core.""QuantityBreak""
                    DROP CONSTRAINT IF EXISTS ""FK_QuantityBreak_ProductPrice_ProductPriceId"";
                ALTER TABLE core.""QuantityBreak""
                    ADD CONSTRAINT ""FK_QuantityBreak_ProductPrice_ProductPriceId""
                    FOREIGN KEY (""ProductPriceId"") REFERENCES core.""ProductPrice"" (""Id"") ON DELETE CASCADE;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""Promotion"" (
                    ""Id""               varchar(50)   NOT NULL PRIMARY KEY,
                    ""Name""             varchar(255)  NULL,
                    ""Code""             varchar(50)   NULL,
                    ""Description""      varchar(4000) NULL,
                    ""ProductId""        varchar(50)   NULL,
                    ""PricePolicyId""    varchar(50)   NULL,
                    ""PromotionalPrice"" float8        NULL,
                    ""DiscountPercent""  float8        NULL,
                    ""StartDate""        timestamp     NULL,
                    ""EndDate""          timestamp     NULL,
                    ""Priority""         int           NOT NULL DEFAULT 0,
                    ""IsActive""         boolean       NOT NULL DEFAULT TRUE,
                    ""IsDeleted""        boolean       NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""     timestamp     NULL,
                    ""CreatedById""      varchar(450)  NULL,
                    ""UpdatedAtUtc""     timestamp     NULL,
                    ""UpdatedById""      varchar(450)  NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_Promotion_IsDeleted""   ON core.""Promotion"" (""IsDeleted"");
                CREATE INDEX IF NOT EXISTS ""IX_Promotion_Name""         ON core.""Promotion"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_Promotion_Code""         ON core.""Promotion"" (""Code"");
                CREATE INDEX IF NOT EXISTS ""IX_Promotion_ProductId""    ON core.""Promotion"" (""ProductId"");
                ALTER TABLE core.""Promotion""
                    DROP CONSTRAINT IF EXISTS ""FK_Promotion_Product_ProductId"";
                ALTER TABLE core.""Promotion""
                    ADD CONSTRAINT ""FK_Promotion_Product_ProductId""
                    FOREIGN KEY (""ProductId"") REFERENCES core.""Product"" (""Id"") ON DELETE SET NULL;
                ALTER TABLE core.""Promotion""
                    DROP CONSTRAINT IF EXISTS ""FK_Promotion_PricePolicy_PricePolicyId"";
                ALTER TABLE core.""Promotion""
                    ADD CONSTRAINT ""FK_Promotion_PricePolicy_PricePolicyId""
                    FOREIGN KEY (""PricePolicyId"") REFERENCES core.""PricePolicy"" (""Id"") ON DELETE SET NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS core.""PriceHistory"" (
                    ""Id""             varchar(50)   NOT NULL PRIMARY KEY,
                    ""ProductPriceId"" varchar(50)   NULL,
                    ""PreviousPrice""  float8        NULL,
                    ""NewPrice""       float8        NULL,
                    ""ChangedById""    varchar(450)  NULL,
                    ""ChangedDate""    timestamp     NOT NULL DEFAULT NOW(),
                    ""ChangeReason""   varchar(4000) NULL,
                    ""IsDeleted""      boolean       NOT NULL DEFAULT FALSE,
                    ""CreatedAtUtc""   timestamp     NULL,
                    ""CreatedById""    varchar(450)  NULL,
                    ""UpdatedAtUtc""   timestamp     NULL,
                    ""UpdatedById""    varchar(450)  NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_PriceHistory_IsDeleted""    ON core.""PriceHistory"" (""IsDeleted"");
                CREATE INDEX IF NOT EXISTS ""IX_PriceHistory_ProductPriceId"" ON core.""PriceHistory"" (""ProductPriceId"");
                CREATE INDEX IF NOT EXISTS ""IX_PriceHistory_ChangedDate""   ON core.""PriceHistory"" (""ChangedDate"");
                ALTER TABLE core.""PriceHistory""
                    DROP CONSTRAINT IF EXISTS ""FK_PriceHistory_ProductPrice_ProductPriceId"";
                ALTER TABLE core.""PriceHistory""
                    ADD CONSTRAINT ""FK_PriceHistory_ProductPrice_ProductPriceId""
                    FOREIGN KEY (""ProductPriceId"") REFERENCES core.""ProductPrice"" (""Id"") ON DELETE SET NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE core.""CustomerGroup"" ADD COLUMN IF NOT EXISTS ""PricePolicyId"" varchar(50) NULL;
                CREATE INDEX IF NOT EXISTS ""IX_CustomerGroup_PricePolicyId""
                    ON core.""CustomerGroup"" (""PricePolicyId"");
                ALTER TABLE core.""CustomerGroup""
                    DROP CONSTRAINT IF EXISTS ""FK_CustomerGroup_PricePolicy_PricePolicyId"";
                ALTER TABLE core.""CustomerGroup""
                    ADD CONSTRAINT ""FK_CustomerGroup_PricePolicy_PricePolicyId""
                    FOREIGN KEY (""PricePolicyId"") REFERENCES core.""PricePolicy"" (""Id"") ON DELETE SET NULL;
            ");
        }
        else
        {
            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Company' AND COLUMN_NAME = 'LogoName')
                BEGIN
                    ALTER TABLE [Company] ADD [LogoName] nvarchar(500) NULL;
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'ImageName')
                BEGIN
                    ALTER TABLE [Product] ADD [ImageName] nvarchar(255) NULL;
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'IsWarrantyApplicable')
                BEGIN
                    ALTER TABLE [Product] ADD [IsWarrantyApplicable] bit NULL;
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'Barcode')
                BEGIN
                    ALTER TABLE [Product] ADD [Barcode] nvarchar(100) NULL;
                    CREATE INDEX [IX_Product_Barcode] ON [Product] ([Barcode]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProductGroup' AND COLUMN_NAME = 'ParentId')
                BEGIN
                    ALTER TABLE [ProductGroup] ADD [ParentId] nvarchar(50) NULL;
                    CREATE INDEX [IX_ProductGroup_ParentId] ON [ProductGroup] ([ParentId]);
                    ALTER TABLE [ProductGroup] ADD CONSTRAINT [FK_ProductGroup_ProductGroup_ParentId]
                        FOREIGN KEY ([ParentId]) REFERENCES [ProductGroup] ([Id]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Brand')
                BEGIN
                    CREATE TABLE [Brand] (
                        [Id]            nvarchar(50)   NOT NULL PRIMARY KEY,
                        [Name]          nvarchar(255)  NULL,
                        [Number]        nvarchar(50)   NULL,
                        [Description]   nvarchar(4000) NULL,
                        [ImageName]     nvarchar(255)  NULL,
                        [Status]        nvarchar(50)   NULL,
                        [IsDeleted]     bit            NOT NULL DEFAULT 0,
                        [CreatedAtUtc]  datetime2      NULL,
                        [CreatedById]   nvarchar(450)  NULL,
                        [UpdatedAtUtc]  datetime2      NULL,
                        [UpdatedById]   nvarchar(450)  NULL
                    );
                    CREATE INDEX [IX_Brand_IsDeleted] ON [Brand] ([IsDeleted]);
                    CREATE UNIQUE INDEX [IX_Brand_Name] ON [Brand] ([Name]);
                    CREATE INDEX [IX_Brand_Number] ON [Brand] ([Number]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'BrandId')
                BEGIN
                    ALTER TABLE [Product] ADD [BrandId] nvarchar(50) NULL;
                    CREATE INDEX [IX_Product_BrandId] ON [Product] ([BrandId]);
                    ALTER TABLE [Product] ADD CONSTRAINT [FK_Product_Brand_BrandId]
                        FOREIGN KEY ([BrandId]) REFERENCES [Brand] ([Id]) ON DELETE SET NULL;
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NavigationMenuSortOrder')
                BEGIN
                    CREATE TABLE [NavigationMenuSortOrder] (
                        [Id]            nvarchar(50)  NOT NULL PRIMARY KEY,
                        [IsDeleted]     bit           NOT NULL DEFAULT 0,
                        [CreatedAtUtc]  datetime2     NULL,
                        [CreatedById]   nvarchar(450) NULL,
                        [UpdatedAtUtc]  datetime2     NULL,
                        [UpdatedById]   nvarchar(450) NULL,
                        [UserId]        nvarchar(450) NULL,
                        [SortOrderJson] nvarchar(max) NULL
                    );
                    CREATE INDEX [IX_NavigationMenuSortOrder_UserId]
                        ON [NavigationMenuSortOrder] ([UserId]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLog')
                BEGIN
                    CREATE TABLE [AuditLog] (
                        [Id]            nvarchar(50)   NOT NULL PRIMARY KEY,
                        [EntityType]    nvarchar(255)  NULL,
                        [EntityId]      nvarchar(50)   NULL,
                        [OperationType] nvarchar(50)   NULL,
                        [OldValues]     nvarchar(max)  NULL,
                        [NewValues]     nvarchar(max)  NULL,
                        [UserId]        nvarchar(450)  NULL,
                        [IpAddress]     nvarchar(50)   NULL,
                        [CreatedAtUtc]  datetime2      NULL
                    );
                    CREATE INDEX [IX_AuditLog_EntityType]   ON [AuditLog] ([EntityType]);
                    CREATE INDEX [IX_AuditLog_UserId]        ON [AuditLog] ([UserId]);
                    CREATE INDEX [IX_AuditLog_CreatedAtUtc]  ON [AuditLog] ([CreatedAtUtc]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserActivityLog')
                BEGIN
                    CREATE TABLE [UserActivityLog] (
                        [Id]           nvarchar(50)   NOT NULL PRIMARY KEY,
                        [UserId]       nvarchar(450)  NULL,
                        [UserEmail]    nvarchar(255)  NULL,
                        [ActivityType] nvarchar(100)  NULL,
                        [Description]  nvarchar(2000) NULL,
                        [PageUrl]      nvarchar(2000) NULL,
                        [IpAddress]    nvarchar(50)   NULL,
                        [UserAgent]    nvarchar(500)  NULL,
                        [CreatedAtUtc] datetime2      NULL
                    );
                    CREATE INDEX [IX_UserActivityLog_UserId]       ON [UserActivityLog] ([UserId]);
                    CREATE INDEX [IX_UserActivityLog_ActivityType]  ON [UserActivityLog] ([ActivityType]);
                    CREATE INDEX [IX_UserActivityLog_CreatedAtUtc]  ON [UserActivityLog] ([CreatedAtUtc]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'WarrantyDays')
                BEGIN
                    ALTER TABLE [Product] ADD [WarrantyDays] int NULL;
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PricePolicy')
                BEGIN
                    CREATE TABLE [PricePolicy] (
                        [Id]            nvarchar(50)   NOT NULL PRIMARY KEY,
                        [Name]          nvarchar(255)  NULL,
                        [Code]          nvarchar(50)   NULL,
                        [Description]   nvarchar(4000) NULL,
                        [Priority]      int            NOT NULL DEFAULT 0,
                        [IsActive]      bit            NOT NULL DEFAULT 1,
                        [EffectiveFrom] datetime2      NULL,
                        [EffectiveTo]   datetime2      NULL,
                        [IsDeleted]     bit            NOT NULL DEFAULT 0,
                        [CreatedAtUtc]  datetime2      NULL,
                        [CreatedById]   nvarchar(450)  NULL,
                        [UpdatedAtUtc]  datetime2      NULL,
                        [UpdatedById]   nvarchar(450)  NULL
                    );
                    CREATE INDEX [IX_PricePolicy_IsDeleted] ON [PricePolicy] ([IsDeleted]);
                    CREATE INDEX [IX_PricePolicy_Name]      ON [PricePolicy] ([Name]);
                    CREATE INDEX [IX_PricePolicy_Code]      ON [PricePolicy] ([Code]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductPrice')
                BEGIN
                    CREATE TABLE [ProductPrice] (
                        [Id]                     nvarchar(50)  NOT NULL PRIMARY KEY,
                        [ProductId]              nvarchar(50)  NULL,
                        [PricePolicyId]          nvarchar(50)  NULL,
                        [CalculationMethod]      int           NOT NULL DEFAULT 0,
                        [FixedPrice]             float         NULL,
                        [MarkupPercent]          float         NULL,
                        [MarkupAmount]           float         NULL,
                        [MarginPercent]          float         NULL,
                        [FormulaMultiplier]      float         NULL,
                        [MinimumSellingPrice]    float         NULL,
                        [MaximumDiscountPercent] float         NULL,
                        [EffectiveFrom]          datetime2     NULL,
                        [EffectiveTo]            datetime2     NULL,
                        [Priority]               int           NOT NULL DEFAULT 0,
                        [IsActive]               bit           NOT NULL DEFAULT 1,
                        [IsDeleted]              bit           NOT NULL DEFAULT 0,
                        [CreatedAtUtc]           datetime2     NULL,
                        [CreatedById]            nvarchar(450) NULL,
                        [UpdatedAtUtc]           datetime2     NULL,
                        [UpdatedById]            nvarchar(450) NULL,
                        CONSTRAINT [FK_ProductPrice_Product_ProductId]
                            FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_ProductPrice_PricePolicy_PricePolicyId]
                            FOREIGN KEY ([PricePolicyId]) REFERENCES [PricePolicy] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_ProductPrice_IsDeleted]     ON [ProductPrice] ([IsDeleted]);
                    CREATE INDEX [IX_ProductPrice_ProductId]     ON [ProductPrice] ([ProductId]);
                    CREATE INDEX [IX_ProductPrice_PricePolicyId] ON [ProductPrice] ([PricePolicyId]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QuantityBreak')
                BEGIN
                    CREATE TABLE [QuantityBreak] (
                        [Id]             nvarchar(50)  NOT NULL PRIMARY KEY,
                        [ProductPriceId] nvarchar(50)  NULL,
                        [MinQuantity]    float         NOT NULL DEFAULT 1,
                        [MaxQuantity]    float         NULL,
                        [Price]          float         NOT NULL DEFAULT 0,
                        [IsDeleted]      bit           NOT NULL DEFAULT 0,
                        [CreatedAtUtc]   datetime2     NULL,
                        [CreatedById]    nvarchar(450) NULL,
                        [UpdatedAtUtc]   datetime2     NULL,
                        [UpdatedById]    nvarchar(450) NULL,
                        CONSTRAINT [FK_QuantityBreak_ProductPrice_ProductPriceId]
                            FOREIGN KEY ([ProductPriceId]) REFERENCES [ProductPrice] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_QuantityBreak_IsDeleted]      ON [QuantityBreak] ([IsDeleted]);
                    CREATE INDEX [IX_QuantityBreak_ProductPriceId] ON [QuantityBreak] ([ProductPriceId]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Promotion')
                BEGIN
                    CREATE TABLE [Promotion] (
                        [Id]               nvarchar(50)   NOT NULL PRIMARY KEY,
                        [Name]             nvarchar(255)  NULL,
                        [Code]             nvarchar(50)   NULL,
                        [Description]      nvarchar(4000) NULL,
                        [ProductId]        nvarchar(50)   NULL,
                        [PricePolicyId]    nvarchar(50)   NULL,
                        [PromotionalPrice] float          NULL,
                        [DiscountPercent]  float          NULL,
                        [StartDate]        datetime2      NULL,
                        [EndDate]          datetime2      NULL,
                        [Priority]         int            NOT NULL DEFAULT 0,
                        [IsActive]         bit            NOT NULL DEFAULT 1,
                        [IsDeleted]        bit            NOT NULL DEFAULT 0,
                        [CreatedAtUtc]     datetime2      NULL,
                        [CreatedById]      nvarchar(450)  NULL,
                        [UpdatedAtUtc]     datetime2      NULL,
                        [UpdatedById]      nvarchar(450)  NULL,
                        CONSTRAINT [FK_Promotion_Product_ProductId]
                            FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_Promotion_PricePolicy_PricePolicyId]
                            FOREIGN KEY ([PricePolicyId]) REFERENCES [PricePolicy] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_Promotion_IsDeleted]  ON [Promotion] ([IsDeleted]);
                    CREATE INDEX [IX_Promotion_Name]        ON [Promotion] ([Name]);
                    CREATE INDEX [IX_Promotion_Code]        ON [Promotion] ([Code]);
                    CREATE INDEX [IX_Promotion_ProductId]   ON [Promotion] ([ProductId]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PriceHistory')
                BEGIN
                    CREATE TABLE [PriceHistory] (
                        [Id]             nvarchar(50)   NOT NULL PRIMARY KEY,
                        [ProductPriceId] nvarchar(50)   NULL,
                        [PreviousPrice]  float          NULL,
                        [NewPrice]       float          NULL,
                        [ChangedById]    nvarchar(450)  NULL,
                        [ChangedDate]    datetime2      NOT NULL DEFAULT GETUTCDATE(),
                        [ChangeReason]   nvarchar(4000) NULL,
                        [IsDeleted]      bit            NOT NULL DEFAULT 0,
                        [CreatedAtUtc]   datetime2      NULL,
                        [CreatedById]    nvarchar(450)  NULL,
                        [UpdatedAtUtc]   datetime2      NULL,
                        [UpdatedById]    nvarchar(450)  NULL,
                        CONSTRAINT [FK_PriceHistory_ProductPrice_ProductPriceId]
                            FOREIGN KEY ([ProductPriceId]) REFERENCES [ProductPrice] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_PriceHistory_IsDeleted]      ON [PriceHistory] ([IsDeleted]);
                    CREATE INDEX [IX_PriceHistory_ProductPriceId] ON [PriceHistory] ([ProductPriceId]);
                    CREATE INDEX [IX_PriceHistory_ChangedDate]    ON [PriceHistory] ([ChangedDate]);
                END
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CustomerGroup' AND COLUMN_NAME = 'PricePolicyId')
                BEGIN
                    ALTER TABLE [CustomerGroup] ADD [PricePolicyId] nvarchar(50) NULL;
                    CREATE INDEX [IX_CustomerGroup_PricePolicyId] ON [CustomerGroup] ([PricePolicyId]);
                    ALTER TABLE [CustomerGroup] ADD CONSTRAINT [FK_CustomerGroup_PricePolicy_PricePolicyId]
                        FOREIGN KEY ([PricePolicyId]) REFERENCES [PricePolicy] ([Id]) ON DELETE SET NULL;
                END
            ");
        }
    }
}


