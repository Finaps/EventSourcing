using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Aggregates.Orders;

public record OrderCreatedEvent(Guid BasketId) : Event();