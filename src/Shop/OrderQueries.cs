using Microsoft.EntityFrameworkCore;

namespace Shop;

public sealed record OrderSummary(int Id, string CustomerName, DateTime PlacedAt, int LineCount, long TotalCents);

public static class OrderQueries
{
    /// <summary>Newest orders first, one page at a time, with line count and total.</summary>
    public static async Task<List<OrderSummary>> ListRecentAsync(ShopContext db, PageRequest page, CancellationToken ct = default)
    {
        var orders = await db.Orders
            .OrderByDescending(o => o.PlacedAt)
            .ThenByDescending(o => o.Id)
            .Skip(page.Offset)
            .Take(page.Size)
            .Select(o => new { o.Id, CustomerName = o.Customer.Name, o.PlacedAt })
            .ToListAsync(ct);

        var result = new List<OrderSummary>();
        foreach (var order in orders)
        {
            var lines = await db.OrderLines.Where(l => l.OrderId == order.Id).ToListAsync(ct);
            result.Add(new OrderSummary(
                order.Id,
                order.CustomerName,
                order.PlacedAt,
                lines.Count,
                lines.Sum(l => l.Quantity * l.UnitPriceCents)));
        }

        return result;
    }

    /// <summary>One order with its lines, read only.</summary>
    public static Task<Order?> GetWithLinesAsync(ShopContext db, int orderId, CancellationToken ct = default) =>
        db.Orders
            .AsNoTracking()
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .SingleOrDefaultAsync(o => o.Id == orderId, ct);
}
