namespace Finaps.EventSourcing.Example.Domain.Baskets;

public record BasketCreatedEvent(TimeSpan ExpirationTime) : Event<Basket>;
public record ProductAddedToBasketEvent(int Quantity, Guid ProductId ) : Event<Basket>;
public record ProductRemovedFromBasketEvent(int Quantity, Guid ProductId) : Event<Basket>;
public record BasketCheckedOutEvent : Event<Basket>;