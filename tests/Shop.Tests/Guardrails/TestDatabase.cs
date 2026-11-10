using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shop.Tests.Guardrails;

/// <summary>
/// One SQLite in-memory database per test class. The connection stays open because
/// the database disappears when its last connection closes.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const int Customers = 3;
    public const int OrdersPerCustomer = 40;

    private readonly SqliteConnection _connection =
        new(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());

    public TestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
        Seed(db);
    }

    /// <summary>A context with the app rules and, when given, a counter of every command.</summary>
    public ShopContext CreateContext(QueryCounter? counter = null)
    {
        var builder = ShopContext.ApplyRules(new DbContextOptionsBuilder<ShopContext>().UseSqlite(_connection));
        if (counter is not null)
        {
            builder.AddInterceptors(counter);
        }

        return new ShopContext(builder.Options);
    }

    public void Dispose() => _connection.Dispose();

    private static void Seed(ShopContext db)
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var c = 1; c <= Customers; c++)
        {
            var customer = new Customer { Name = $"Customer {c}" };
            for (var o = 1; o <= OrdersPerCustomer; o++)
            {
                var order = new Order { PlacedAt = start.AddHours(o * Customers + c) };

                // One to three lines per order, always the same for the same order.
                for (var l = 1; l <= 1 + (o % 3); l++)
                {
                    order.Lines.Add(new OrderLine { Product = $"Product {l}", Quantity = l, UnitPriceCents = 250 * l });
                }

                customer.Orders.Add(order);
            }

            db.Customers.Add(customer);
        }

        db.SaveChanges();
    }
}
