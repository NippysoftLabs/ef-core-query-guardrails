namespace Shop;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Order> Orders { get; } = [];
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime PlacedAt { get; set; }
    public List<OrderLine> Lines { get; } = [];
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string Product { get; set; }
    public int Quantity { get; set; }

    // Money in cents: SQLite cannot aggregate decimal columns on the server.
    public long UnitPriceCents { get; set; }
}
