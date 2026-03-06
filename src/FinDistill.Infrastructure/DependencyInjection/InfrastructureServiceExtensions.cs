using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Caching;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.DataMarts;
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
        // Options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<FeaturesOptions>(configuration.GetSection(FeaturesOptions.SectionName));

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

        // Data Mart reader
        // Default: Dapper. ClickHouse support will be added in Phase 11.
        services.AddScoped<IDataMartReader, DapperDataMartReader>();

        // Cache service
        // Default: NullCacheService (no-op). Redis support will be added in Phase 10.
        services.AddSingleton<ICacheService, NullCacheService>();

        // API providers (registered as IMarketDataProvider for IEnumerable<IMarketDataProvider> injection)
        services.AddHttpClient<YahooFinanceProvider>();
        services.AddHttpClient<CoinGeckoProvider>();
        services.AddScoped<IMarketDataProvider, YahooFinanceProvider>();
        services.AddScoped<IMarketDataProvider, CoinGeckoProvider>();

        return services;
    }
}
