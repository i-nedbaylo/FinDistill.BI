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
        // then add separate IMarketDataProvider registrations for IEnumerable<IMarketDataProvider> injection.
        // Using AddHttpClient<IMarketDataProvider, T> would cause the second call to overwrite the first.
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
        services.AddScoped<IMarketDataProvider, YahooFinanceProvider>();
        services.AddScoped<IMarketDataProvider, CoinGeckoProvider>();

        // Ticker provider (reads DataSources config)
        services.AddSingleton<ITickerProvider, ConfigTickerProvider>();

        return services;
    }
}
