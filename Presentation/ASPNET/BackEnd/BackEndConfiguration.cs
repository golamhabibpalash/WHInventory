using Application;
using Application.Common.Services.BusinessClockManager;
using Application.Features.NotificationManager;
using ASPNET.BackEnd.Common.Filters;
using ASPNET.BackEnd.Common.Handlers;
using ASPNET.BackEnd.Common.Helpers;
using ASPNET.BackEnd.Common.Services;
using ASPNET.BackEnd.Hubs;
using Infrastructure;
using Infrastructure.DataAccessManager.EFCore;
using Infrastructure.SeedManager;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization.Metadata;
using System.Threading.RateLimiting;

namespace ASPNET.BackEnd;

public static class BackEndConfiguration
{
    public static IServiceCollection AddBackEndServices(this IServiceCollection services, IConfiguration configuration)
    {
        //>>> Application Layer
        services.AddApplicationServices();

        //>>> Infrastructure Layer
        services.AddInfrastructureServices(configuration);

        services.AddExceptionHandler<CustomExceptionHandler>();

        //>>> Common

        services.AddHttpContextAccessor();
        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
        });
        services.AddControllers(options =>
            {
                options.Filters.Add<AuditFieldActionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
                // Append rather than replace: assigning TypeInfoResolver directly would drop the
                // framework's default resolver chain (polymorphism, cycles, etc.).
                options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { UtcDateTimeJsonModifier.Apply }
                });
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "uStock API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

        });


        services.Configure<ApiBehaviorOptions>(x =>
        {
            x.SuppressModelStateInvalidFilter = true;
        });

        //>>> Live notifications (SignalR + per-user groups)
        services.AddSignalR();
        services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();

        //>>> Business-timezone clock (appsettings.json -> TimeZoneId)
        services.AddSingleton<IBusinessClock, BusinessClock>();

        //>>> Real client IP behind the Cloudflare tunnel (compose exposes no direct app port), so
        //>>> per-IP rate limiting partitions by the actual caller rather than the proxy.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        //>>> Throttle anonymous auth endpoints (login/register/password reset/refresh) to blunt
        //>>> brute-force and enumeration. Per-account password attempts are already locked out by
        //>>> ASP.NET Identity; this caps request volume per client IP as well.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    //>>> Rate-limit policy name applied to the sensitive anonymous auth endpoints.
    public const string AuthRateLimitPolicy = "auth";

    public static IEndpointRouteBuilder MapBackEndRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        return endpoints;
    }

    public static IApplicationBuilder RegisterBackEndBuilder(
        this IApplicationBuilder app,
        IWebHostEnvironment environment,
        IHost host,
        IConfiguration configuration
        )
    {
        // >>> Refuse to boot a non-Development host that still carries the repo's placeholder
        // secrets. The JWT signing key is symmetric (HS256): if the committed default survives
        // into production, anyone can forge a token with any TenantId claim and read or modify
        // every tenant's data, defeating the EF global tenant filters entirely. Fail closed.
        GuardProductionSecrets(environment, configuration);

        // >>> Create database
        host.CreateDatabase();

        //seed database with system data
        host.SeedSystemData();

        //seed database with demo data
        // Demo/sample data must never land in a production database, even if the flag is left on
        // (the repo ships IsDemoVersion=true for developer convenience and there is no guarantee a
        // production deployment overrides it). appsettings.Production.json also sets it false; this
        // is the belt-and-braces guard that holds even when that file is absent.
        if (configuration.GetValue<bool>("IsDemoVersion") && !environment.IsProduction())
        {
            host.SeedDemoData();
        }

        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "uStock V1");
            });
        }

        return app;
    }

    // Known-insecure values shipped in the repo's appsettings.json. These are safe only in
    // Development; any other environment must override them with real secrets.
    private const string PlaceholderJwtKey = "YourVeryStrongAndSecureSecretKeyWhichIs32Chars!";
    private const string PlaceholderAdminPassword = "123456";

    private static void GuardProductionSecrets(IWebHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var violations = new List<string>();

        if (string.Equals(configuration["Jwt:Key"], PlaceholderJwtKey, StringComparison.Ordinal))
        {
            violations.Add("Jwt:Key is still the repo placeholder. Set a unique secret (>= 32 chars) via environment/secret store.");
        }

        if (string.Equals(configuration["AspNetIdentity:DefaultAdmin:Password"], PlaceholderAdminPassword, StringComparison.Ordinal))
        {
            violations.Add("AspNetIdentity:DefaultAdmin:Password is still the default '123456'. Set a strong password.");
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Refusing to start in the '{environment.EnvironmentName}' environment with insecure default secrets:"
                + Environment.NewLine + " - " + string.Join(Environment.NewLine + " - ", violations));
        }
    }

}
