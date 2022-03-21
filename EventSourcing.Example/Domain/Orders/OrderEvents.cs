namespace EventSourcing.Example.Domain.Orders;

public record OrderCreatedEvent(Guid BasketId) : Event<Order>;