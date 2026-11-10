using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    /// <summary>
    /// Fixed rules shared by the app and the tests. Each listed warning becomes an exception.
    /// </summary>
    public static DbContextOptionsBuilder<ShopContext> ApplyRules(DbContextOptionsBuilder<ShopContext> builder) =>
        builder.ConfigureWarnings(warnings => warnings.Throw(
            // Skip/Take without OrderBy: pages could repeat or skip rows.
            CoreEventId.RowLimitingOperationWithoutOrderByWarning,
            // First/FirstOrDefault without OrderBy and without a filter: the row is arbitrary.
            CoreEventId.FirstWithoutOrderByAndFilterWarning));
}
