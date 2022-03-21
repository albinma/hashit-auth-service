using Duende.IdentityServer.EntityFramework.Services;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Used for configuring the web host.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures services for the web host.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        Log.Information("Configuring application services");

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

        builder.Services.AddEndpointsApiExplorer();

        // Configure caching
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
        });

        // Configure health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("Postgres"))
            .AddRedis(builder.Configuration.GetConnectionString("Redis"));

        // Configure identity server
        builder.AdddentityServer();

        return builder.Build();
    }

    public static WebApplicationBuilder AdddentityServer(this WebApplicationBuilder builder)
    {
        Log.Information("Configuring Identity Server");
        var migrationAssembly = typeof(Program).Assembly.GetName().Name;

        // Override caches to use distributed cache
        builder.Services.AddTransient(typeof(ICache<>), typeof(DistributedCache<>));

        builder.Services.AddIdentityServer()
            .AddDeveloperSigningCredential()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = x =>
                    x.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
                    sql => sql.MigrationsAssembly(migrationAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.EnableTokenCleanup = true;
                options.TokenCleanupInterval = 3600;
                options.TokenCleanupBatchSize = 100;
                options.ConfigureDbContext = x =>
                    x.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
                    sql => sql.MigrationsAssembly(migrationAssembly));
            })
            .AddClientStoreCache<ClientStore>()
            .AddCorsPolicyCache<CorsPolicyService>()
            .AddResourceStoreCache<ResourceStore>()
            .AddIdentityProviderStoreCache<IdentityProviderStore>();

        return builder;
    }

    /// <summary>
    /// Configures the web host pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        Log.Information("Configuring application pipeline");

        app.UseSerilogRequestLogging();

        app.UseIdentityServer();

        app.UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            AllowCachingResponses = false,
            ResponseWriter = (ctx, report) => ctx.Response.WriteAsJsonAsync(new HealthCheckModel(report))
        });

        app.MapControllers();

        return app;
    }
}
