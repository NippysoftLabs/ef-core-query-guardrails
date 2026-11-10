using Shop.Tests.Guardrails;
using Xunit;

namespace Shop.Tests;

/// <summary>
/// Three checks per use case: the SQL matches the approved snapshot, the number of
/// commands stays within its budget, and a read leaves nothing tracked.
/// </summary>
public sealed class OrderQueryTests(TestDatabase database) : IClassFixture<TestDatabase>
{
    private const int ListRecentBudget = 1;
    private const int GetWithLinesBudget = 1;

    [Fact]
    public async Task ListRecent_sql_matches_snapshot()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        // Page 3: offset 20 and size 10 differ. With equal values EF Core 10 sends one parameter for both.
        await OrderQueries.ListRecentAsync(db, new PageRequest(3, 10));

        SnapshotAssert.Matches("ListRecent", counter.Commands);
    }

    [Fact]
    public async Task ListRecent_stays_within_query_budget()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        await OrderQueries.ListRecentAsync(db, new PageRequest(1, 10));

        Assert.Equal(ListRecentBudget, counter.Count);
    }

    [Fact]
    public async Task ListRecent_tracks_nothing()
    {
        await using var db = database.CreateContext();

        await OrderQueries.ListRecentAsync(db, new PageRequest(1, 10));

        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ListRecent_returns_one_page_newest_first()
    {
        await using var db = database.CreateContext();

        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(1, 10));

        Assert.Equal(10, page.Count);

        // The seed places the last order of the last customer latest.
        Assert.Equal(TestDatabase.Customers * TestDatabase.OrdersPerCustomer, page[0].Id);
        Assert.Equal(page.OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id), page);
    }

    [Fact]
    public async Task GetWithLines_sql_matches_snapshot()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        await OrderQueries.GetWithLinesAsync(db, 7);

        SnapshotAssert.Matches("GetWithLines", counter.Commands);
    }

    [Fact]
    public async Task GetWithLines_stays_within_query_budget()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        await OrderQueries.GetWithLinesAsync(db, 7);

        Assert.Equal(GetWithLinesBudget, counter.Count);
    }

    [Fact]
    public async Task GetWithLines_tracks_nothing()
    {
        await using var db = database.CreateContext();

        var order = await OrderQueries.GetWithLinesAsync(db, 7);

        Assert.NotNull(order);
        Assert.NotEmpty(order.Lines);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
