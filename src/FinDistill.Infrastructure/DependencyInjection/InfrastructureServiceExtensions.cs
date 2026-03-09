using Dapper;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Caching;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.DataMarts;
using FinDistill.Infrastructure.Http;
using FinDistill.Infrastructure.Persistence;
using FinDistill.Infrastructure.Providers;
using FinDistill.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinDistill.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers DbContext, repositories, API providers, Data Mart reader, and cache service.
    /// Provider and feature selection are driven by <c>Database:Provider</c> and <c>Features</c> config sections.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration for connection strings and options.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Dapper type handlers
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        // Options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<FeaturesOptions>(configuration.GetSection(FeaturesOptions.SectionName));
        services.Configure<DataSourcesOptions>(configuration.GetSection(DataSourcesOptions.SectionName));

        var dbOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(dbOptions);

        var features = new FeaturesOptions();
        configuration.GetSection(FeaturesOptions.SectionName).Bind(features);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        // DbContext — provider selection
        services.AddDbContext<FinDistillDbContext>(options =>
        {
            switch (dbOptions.Provider)
            {
                case "SqlServer":
                    options.UseSqlServer(connectionString);
                    break;
                case "PostgreSQL":
                    options.UseNpgsql(connectionString);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider: '{dbOptions.Provider}'. Set 'Database:Provider' to 'SqlServer' or 'PostgreSQL'.");
            }
        });

        // Dapper connection factory
        services.AddSingleton<DapperConnectionFactory>();

        // Repositories (Scoped — aligned with DbContext lifetime)
        services.AddScoped<IRawIngestDataRepository, RawIngestDataRepository>();
        services.AddScoped<IDimAssetRepository, DimAssetRepository>();
        services.AddScoped<IDimDateRepository, DimDateRepository>();
        services.AddScoped<IDimSourceRepository, DimSourceRepository>();
        services.AddScoped<IFactQuoteRepository, FactQuoteRepository>();

        // Data Mart reader — ClickHouse or Dapper
        if (features.UseClickHouse)
        {
            var clickHouseCs = configuration.GetConnectionString("ClickHouse")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:ClickHouse is required when Features:UseClickHouse is true.");

            services.Configure<ClickHouseOptions>(o => o.ConnectionString = clickHouseCs);
            services.AddScoped<IDataMartReader, ClickHouseDataMartReader>();
            services.AddScoped<IClickHouseSyncService, ClickHouseSyncService>();
        }
        else
        {
            services.AddScoped<IDataMartReader, DapperDataMartReader>();
        }

        // Cache service
        // Default: NullCacheService (no-op). Redis support will be added in Phase 10.
        services.AddSingleton<ICacheService, NullCacheService>();

        // Retry handler for HTTP clients
        services.AddTransient<RetryDelegatingHandler>();

        // API providers: register each under its concrete type for IHttpClientFactory pipeline,
        // then add IMarketDataProvider registrations that resolve through the factory.
        // Using AddScoped<IMarketDataProvider, T> directly would bypass the HttpClient pipeline
        // (RetryDelegatingHandler, default headers) because DI would inject a plain HttpClient.
        services.AddHttpClient<YahooFinanceProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinDistill.BI/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<RetryDelegatingHandler>();
        services.AddHttpClient<CoinGeckoProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FinDistill.BI/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<RetryDelegatingHandler>();

        // Resolve IMarketDataProvider through the concrete type so that each provider
        // receives the HttpClient created by IHttpClientFactory (with retry handler attached).
        services.AddScoped<IMarketDataProvider>(sp => sp.GetRequiredService<YahooFinanceProvider>());
        services.AddScoped<IMarketDataProvider>(sp => sp.GetRequiredService<CoinGeckoProvider>());

        // Ticker provider (reads DataSources config)
        services.AddSingleton<ITickerProvider, ConfigTickerProvider>();

        return services;
    }
}
