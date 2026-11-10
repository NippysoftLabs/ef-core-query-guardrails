using Shop.Tests.Guardrails;
using Xunit;

namespace Shop.Tests;

public sealed class OrderTotalsTests(TestDatabase database) : IClassFixture<TestDatabase>
{
    [Fact]
    public async Task ListRecent_returns_line_count_and_total()
    {
        await using var db = database.CreateContext();

        var page = await OrderQueries.ListRecentAsync(db, new PageRequest(1, 10));

        foreach (var summary in page)
        {
            var order = await OrderQueries.GetWithLinesAsync(db, summary.Id);
            Assert.Equal(order!.Lines.Count, summary.LineCount);
            Assert.Equal(order.Lines.Sum(l => l.Quantity * l.UnitPriceCents), summary.TotalCents);
        }
    }
}
