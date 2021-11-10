using System;
using EventSourcing.Example.Commands;

namespace EventSourcing.Example.Domain.Orders
{
    public record CreateOrder(Guid AggregateId, Guid BasketId) : CommandBase(AggregateId);
}