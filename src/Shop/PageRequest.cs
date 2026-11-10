namespace Shop;

/// <summary>
/// Every list query takes a page, so no list can return the whole table.
/// </summary>
public sealed record PageRequest
{
    public const int MaxSize = 100;

    public int Number { get; }
    public int Size { get; }

    public PageRequest(int number, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaxSize);

        // Checked so a huge page number fails here instead of wrapping to a negative offset.
        _ = checked((number - 1) * size);

        Number = number;
        Size = size;
    }

    public int Offset => (Number - 1) * Size;
}
