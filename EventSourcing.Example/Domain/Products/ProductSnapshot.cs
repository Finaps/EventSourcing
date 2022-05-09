namespace Finaps.EventSourcing.Example.Domain.Products;

// It makes sense to snapshot the Product aggregate once in a while since it can contain many irrelevant events that
// do not influence the current state (e.g. ProductReservedEvent)
public record ProductSnapshot : Snapshot<Product>
{
    public string? Name { get; init; }
    public int Quantity { get; init; }
    public ICollection<Reservation>? Reservations { get; init; }

    // Parameterless constructor is needed by EF for records containing nested entities
    public ProductSnapshot() { }
}