using System;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Orders
{
    public record CreateOrder(Guid AggregateId, Guid BasketId) : CommandBase(AggregateId);
}