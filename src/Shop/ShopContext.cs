using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    /// <summary>
    /// Fixed rules. Call it wherever the context is configured: on the typed builder, as
    /// TestDatabase does, or inside AddDbContext, which passes an untyped builder.
    /// Each listed warning becomes an exception. Returns the same builder.
    /// </summary>
    public static TBuilder ApplyRules<TBuilder>(TBuilder builder)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.ConfigureWarnings(warnings => warnings.Throw(
            // Skip/Take without OrderBy: pages could repeat or skip rows.
            CoreEventId.RowLimitingOperationWithoutOrderByWarning,
            // First/FirstOrDefault without OrderBy and without a filter: the row is arbitrary.
            CoreEventId.FirstWithoutOrderByAndFilterWarning));
        return builder;
    }
}
