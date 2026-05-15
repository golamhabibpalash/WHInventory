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
        }
        else
        {
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
        }
    }
}


