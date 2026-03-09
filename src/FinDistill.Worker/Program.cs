using FinDistill.Application.Configuration;
using FinDistill.Application.DependencyInjection;
using FinDistill.Infrastructure.DependencyInjection;
using FinDistill.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Serilog
    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day));

    // DI registration
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();

    // ETL schedule options
    builder.Services.Configure<EtlScheduleOptions>(
        builder.Configuration.GetSection(EtlScheduleOptions.SectionName));

    // Worker
    builder.Services.AddHostedService<EtlWorker>();

    var host = builder.Build();

    Log.Information("ETL Worker host starting");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ETL Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
