using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Aggregates.Baskets
{
    public record BasketCreatedEvent : Event;
    public record ProductAddedToBasketEvent(int Quantity, Guid ProductId ) : Event;
    public record ProductRemovedFromBasketEvent(int Quantity, Guid ProductId) : Event;
    public record BasketCheckedOutEvent : Event;
}