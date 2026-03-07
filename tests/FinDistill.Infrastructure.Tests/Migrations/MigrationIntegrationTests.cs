using FinDistill.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Tests.Migrations;

[Collection("SqlServer")]
public class MigrationIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public MigrationIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task MigrateAsync_DoesNotThrow()
    {
        await using var context = _fixture.CreateDbContext();

        // Migrations are applied in fixture StartAsync — verify no pending migrations remain
        var pending = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }

    [DockerAvailableFact]
    public async Task Database_HasLakeSchema()
    {
        await using var context = _fixture.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SCHEMA_ID('lake')";
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.NotEqual(DBNull.Value, result);
    }

    [DockerAvailableFact]
    public async Task Database_HasDwhSchema()
    {
        await using var context = _fixture.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SCHEMA_ID('dwh')";
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.NotEqual(DBNull.Value, result);
    }

    [DockerAvailableFact]
    public async Task Database_HasMartSchema()
    {
        await using var context = _fixture.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SCHEMA_ID('mart')";
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.NotEqual(DBNull.Value, result);
    }

    [DockerAvailableFact]
    public async Task Database_HasMartViews()
    {
        await using var context = _fixture.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var viewNames = new[] { "v_DailyPerformance", "v_AssetHistory", "v_PortfolioSummary" };

        foreach (var viewName in viewNames)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT OBJECT_ID('mart.{viewName}', 'V')";
            var result = await cmd.ExecuteScalarAsync();

            Assert.NotNull(result);
            Assert.NotEqual(DBNull.Value, result);
        }
    }
}
