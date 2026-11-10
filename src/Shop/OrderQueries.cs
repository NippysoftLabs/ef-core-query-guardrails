using Microsoft.EntityFrameworkCore;

namespace Shop;

public sealed record OrderSummary(int Id, string CustomerName, DateTime PlacedAt);

public static class OrderQueries
{
    /// <summary>Newest orders first, one page at a time.</summary>
    public static Task<List<OrderSummary>> ListRecentAsync(ShopContext db, PageRequest page, CancellationToken ct = default) =>
        db.Orders
            .OrderByDescending(o => o.PlacedAt)
            .ThenByDescending(o => o.Id)
            .Skip(page.Offset)
            .Take(page.Size)
            .Select(o => new OrderSummary(o.Id, o.Customer.Name, o.PlacedAt))
            .ToListAsync(ct);

    /// <summary>One order with its lines, read only.</summary>
    public static Task<Order?> GetWithLinesAsync(ShopContext db, int orderId, CancellationToken ct = default) =>
        db.Orders
            .AsNoTracking()
            .Include(o => o.Lines.OrderBy(l => l.Id))
            .SingleOrDefaultAsync(o => o.Id == orderId, ct);
}
