using FinDistill.Application.Configuration;
using FinDistill.Application.DependencyInjection;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.DependencyInjection;
using FinDistill.Infrastructure.Persistence;
using FinDistill.Web;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Render.com passes DATABASE_URL as postgres://user:pass@host:port/db
    // Convert to Npgsql connection string format if present
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length >= 2 && !string.IsNullOrEmpty(userInfo[0]))
        {
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = Uri.UnescapeDataString(userInfo[1]);
            var npgsqlCs = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = npgsqlCs;
        }
        else
        {
            Log.Warning("DATABASE_URL is present but does not contain valid user information");
        }
    }

    // Serilog — file sink only outside container environments
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console();

        // Avoid file logging on ephemeral/read-only container filesystems
        if (!context.HostingEnvironment.IsProduction())
            configuration.WriteTo.File("logs/web-.log", rollingInterval: RollingInterval.Day);
    });

    // DI registration
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddControllersWithViews();

    // In-process ETL worker (for single-process deployments, e.g. free hosting tiers)
    var features = builder.Configuration
        .GetSection(FeaturesOptions.SectionName)
        .Get<FeaturesOptions>() ?? new FeaturesOptions();

    if (features.RunEtlInProcess)
    {
        builder.Services.Configure<EtlScheduleOptions>(
            builder.Configuration.GetSection(EtlScheduleOptions.SectionName));
        builder.Services.AddHostedService<InProcessEtlWorker>();
        Log.Information("In-process ETL worker enabled (Features:RunEtlInProcess = true)");
    }

    var app = builder.Build();

    // Auto-migrate on startup — guarded by config flag (Database:AutoMigrate)
    var dbOptions = builder.Configuration
        .GetSection(DatabaseOptions.SectionName)
        .Get<DatabaseOptions>() ?? new DatabaseOptions();

    if (dbOptions.AutoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinDistillDbContext>();
        await db.Database.MigrateAsync();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Health check endpoint for Railway / Render
    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

    // Skip HTTPS redirection in containerised deployments — TLS is terminated at the reverse proxy
    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");
        
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
