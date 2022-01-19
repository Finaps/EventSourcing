using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Baskets;

public record BasketCreatedEvent(TimeSpan ExpirationTime) : Event;
public record ProductAddedToBasketEvent(int Quantity, Guid ProductId ) : Event;
public record ProductRemovedFromBasketEvent(int Quantity, Guid ProductId) : Event;
public record BasketCheckedOutEvent : Event;