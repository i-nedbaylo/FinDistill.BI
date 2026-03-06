using FinDistill.Application.DependencyInjection;
using FinDistill.Infrastructure.DependencyInjection;
using FinDistill.Worker;
using FinDistill.Worker.Configuration;
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

    // Hosting mode
    var hostingMode = builder.Configuration["HostingMode"] ?? "Console";
    if (string.Equals(hostingMode, "WindowsService", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "FinDistill.ETL";
        });
    }

    // DI registration
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();

    // ETL schedule options
    builder.Services.Configure<EtlScheduleOptions>(
        builder.Configuration.GetSection(EtlScheduleOptions.SectionName));

    // Worker
    builder.Services.AddHostedService<EtlWorker>();

    var host = builder.Build();

    Log.Information("ETL Worker host starting. HostingMode: {HostingMode}", hostingMode);
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
