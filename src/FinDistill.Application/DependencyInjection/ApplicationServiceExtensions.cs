using FinDistill.Application.Interfaces;
using FinDistill.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinDistill.Application.DependencyInjection;

/// <summary>
/// Registers all Application layer services into the DI container.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers ETL pipeline services and <see cref="IDashboardService"/> as Scoped.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ETL pipeline services
        services.AddScoped<IExtractorService, ExtractorService>();
        services.AddScoped<ITransformerService, TransformerService>();
        services.AddScoped<ILoaderService, LoaderService>();
        services.AddScoped<IEtlOrchestrator, EtlOrchestrator>();

        // Dashboard service
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
