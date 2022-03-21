namespace EventSourcing.Example.Domain.Products;

public record ProductCreatedEvent(string Name, int Quantity) : Event<Product>;
public record ProductReservedEvent(int Quantity, Guid BasketId, TimeSpan HeldFor) : Event<Product>;
public record ReservationRemovedEvent(int Quantity, Guid BasketId) : Event<Product>;
public record ProductSoldEvent(int Quantity, Guid BasketId) : Event<Product>;
public record ProductStockAddedEvent(int Quantity) : Event<Product>;