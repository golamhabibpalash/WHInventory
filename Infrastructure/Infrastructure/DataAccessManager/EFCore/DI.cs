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
        dataContext.Database.EnsureCreated();

        EnsureMissingTables(dataContext);

        return host;
    }

    private static void EnsureMissingTables(DataContext dataContext)
    {
        var isPostgres = dataContext.Database.IsNpgsql();

        if (isPostgres)
        {
            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Company"" ADD COLUMN IF NOT EXISTS ""LogoName"" varchar(500) NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Product"" ADD COLUMN IF NOT EXISTS ""ImageName"" varchar(255) NULL;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""ProductGroup"" ADD COLUMN IF NOT EXISTS ""ParentId"" varchar(50) NULL;
                CREATE INDEX IF NOT EXISTS ""IX_ProductGroup_ParentId""
                    ON ""ProductGroup"" (""ParentId"");
                ALTER TABLE ""ProductGroup"" DROP CONSTRAINT IF EXISTS ""FK_ProductGroup_ProductGroup_ParentId"";
                ALTER TABLE ""ProductGroup"" ADD CONSTRAINT ""FK_ProductGroup_ProductGroup_ParentId""
                    FOREIGN KEY (""ParentId"") REFERENCES ""ProductGroup"" (""Id"") ON DELETE RESTRICT;
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Brand"" (
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
                CREATE INDEX IF NOT EXISTS ""IX_Brand_IsDeleted"" ON ""Brand"" (""IsDeleted"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Brand_Name"" ON ""Brand"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_Brand_Number"" ON ""Brand"" (""Number"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""NavigationMenuSortOrder"" (
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
                    ON ""NavigationMenuSortOrder"" (""UserId"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""AuditLog"" (
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
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_EntityType"" ON ""AuditLog"" (""EntityType"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_UserId""     ON ""AuditLog"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_AuditLog_CreatedAtUtc"" ON ""AuditLog"" (""CreatedAtUtc"");
            ");

            dataContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""UserActivityLog"" (
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
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_UserId""       ON ""UserActivityLog"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_ActivityType""  ON ""UserActivityLog"" (""ActivityType"");
                CREATE INDEX IF NOT EXISTS ""IX_UserActivityLog_CreatedAtUtc""  ON ""UserActivityLog"" (""CreatedAtUtc"");
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
        }
    }
}


