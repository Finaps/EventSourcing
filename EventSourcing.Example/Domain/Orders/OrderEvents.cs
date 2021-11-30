using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Orders
{
    public record OrderCreatedEvent(Guid BasketId) : Event();
}