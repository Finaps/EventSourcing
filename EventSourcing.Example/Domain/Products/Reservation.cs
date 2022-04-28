namespace Finaps.EventSourcing.Example.Domain.Products;

public record Reservation(int Quantity, Guid BasketId);