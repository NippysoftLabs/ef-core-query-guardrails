using Microsoft.EntityFrameworkCore;
using Shop.Tests.Guardrails;
using Xunit;

namespace Shop.Tests;

/// <summary>The fixed rules fail the query itself, before any snapshot is compared.</summary>
public sealed class RuleTests(TestDatabase database) : IClassFixture<TestDatabase>
{
    [Fact]
    public async Task Take_without_OrderBy_throws()
    {
        await using var db = database.CreateContext();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Orders.Skip(10).Take(10).ToListAsync());

        Assert.Contains("RowLimitingOperationWithoutOrderByWarning", error.Message);
    }

    [Fact]
    public async Task First_without_OrderBy_or_filter_throws()
    {
        await using var db = database.CreateContext();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Orders.FirstAsync());

        Assert.Contains("FirstWithoutOrderByAndFilterWarning", error.Message);
    }

    [Fact]
    public async Task Client_side_filter_throws()
    {
        await using var db = database.CreateContext();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Customers.Where(c => IsVip(c.Name)).ToListAsync());

        Assert.Contains("could not be translated", error.Message);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, PageRequest.MaxSize + 1)]
    [InlineData(-1, 10)]
    public void PageRequest_rejects_out_of_range_values(int number, int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(number, size));

    private static bool IsVip(string name) => name.EndsWith('1');
}
