using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace FinDistill.Infrastructure.Tests.Fixtures;

public class SqlServerContainerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container is not started.");

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_P@ssw0rd!")
            .Build();

        await _container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public FinDistillDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinDistillDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new FinDistillDbContext(options);
    }
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>;
