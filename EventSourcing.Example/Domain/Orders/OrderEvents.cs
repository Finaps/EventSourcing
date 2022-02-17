using System;
using EventSourcing.Core;
using EventSourcing.Core.Records;

namespace EventSourcing.Example.Domain.Orders;

public record OrderCreatedEvent(Guid BasketId) : Event;