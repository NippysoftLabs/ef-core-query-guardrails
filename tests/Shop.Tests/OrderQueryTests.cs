using Shop.Tests.Guardrails;
using Xunit;

namespace Shop.Tests;

/// <summary>
/// Three checks per use case: the SQL matches the approved snapshot, the number of
/// commands stays within its budget, and a read leaves nothing tracked.
/// Each check first asserts that the query returned rows: an N+1 sends one command per row,
/// so on an empty page it sends a single command and tracks nothing.
/// </summary>
public sealed class OrderQueryTests(TestDatabase database) : IClassFixture<TestDatabase>
{
    private const int ListRecentBudget = 1;
    private const int GetWithLinesBudget = 1;
    private const int PageSize = 10;

    [Fact]
    public async Task ListRecent_sql_matches_snapshot()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        // Page 3: offset 20 and size 10 differ. With equal values, EF Core 10.0.14 on SQLite sent one parameter for both.
        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(3, PageSize));

        AssertFullPage(page);
        SnapshotAssert.Matches("ListRecent", counter.Commands);
    }

    [Fact]
    public async Task ListRecent_stays_within_query_budget()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(1, PageSize));

        AssertFullPage(page);
        Assert.Equal(ListRecentBudget, counter.Count);
    }

    [Fact]
    public async Task ListRecent_tracks_nothing()
    {
        await using var db = database.CreateContext();

        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(1, PageSize));

        AssertFullPage(page);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ListRecent_returns_one_page_newest_first()
    {
        await using var db = database.CreateContext();

        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(1, PageSize));

        AssertFullPage(page);

        // The seed places the last order of the last customer latest.
        Assert.Equal(TestDatabase.Customers * TestDatabase.OrdersPerCustomer, page[0].Id);
        Assert.Equal(page.OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id), page);
    }

    [Fact]
    public async Task GetWithLines_sql_matches_snapshot()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        var order = await OrderQueries.GetWithLinesAsync(db, 7);

        AssertHasLines(order);
        SnapshotAssert.Matches("GetWithLines", counter.Commands);
    }

    [Fact]
    public async Task GetWithLines_stays_within_query_budget()
    {
        var counter = new QueryCounter();
        await using var db = database.CreateContext(counter);

        var order = await OrderQueries.GetWithLinesAsync(db, 7);

        AssertHasLines(order);
        Assert.Equal(GetWithLinesBudget, counter.Count);
    }

    [Fact]
    public async Task GetWithLines_tracks_nothing()
    {
        await using var db = database.CreateContext();

        var order = await OrderQueries.GetWithLinesAsync(db, 7);

        AssertHasLines(order);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static void AssertFullPage(List<OrderSummary> page) =>
        Assert.True(page.Count == PageSize, $"Expected a full page of {PageSize} rows, got {page.Count}. Seed enough data for the page the test asks for.");

    private static void AssertHasLines(Order? order)
    {
        Assert.NotNull(order);
        Assert.True(order.Lines.Count > 0, "Expected an order with lines. Seed lines for the order the test asks for.");
    }
}
