namespace Finaps.EventSourcing.Example.Domain.Products;

// It makes sense to snapshot the Product aggregate once in a while since it can contain many irrelevant events that
// do not influence the current state (e.g. ProductReservedEvent)
public record ProductSnapshot : Snapshot<Product>
{
    public string? Name { get; }
    public int Quantity { get; }
    public readonly ICollection<Reservation>? Reservations;

    public ProductSnapshot(string? name, int quantity, ICollection<Reservation> reservations)
    {
        Name = name;
        Quantity = quantity;
        Reservations = reservations;
    }
    
    // Parameterless constructor is needed by EF for records containing nested entities
    private ProductSnapshot() { }
}