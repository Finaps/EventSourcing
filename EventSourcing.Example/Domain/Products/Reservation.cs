namespace EventSourcing.Example.Domain.Products;

public record Reservation(int Quantity, Guid BasketId);