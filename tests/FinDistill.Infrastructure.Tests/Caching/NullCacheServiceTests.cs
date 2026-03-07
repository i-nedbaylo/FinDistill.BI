using FinDistill.Infrastructure.Caching;

namespace FinDistill.Infrastructure.Tests.Caching;

public class NullCacheServiceTests
{
    private readonly NullCacheService _sut = new();

    [Fact]
    public async Task GetAsync_AlwaysReturnsNull()
    {
        var result = await _sut.GetAsync<string>("any-key", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
            _sut.SetAsync("key", "value", TimeSpan.FromMinutes(5), CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RemoveAsync_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
            _sut.RemoveAsync("key", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAsync_WithComplexType_ReturnsNull()
    {
        var result = await _sut.GetAsync<List<int>>("complex-key", CancellationToken.None);

        Assert.Null(result);
    }
}
