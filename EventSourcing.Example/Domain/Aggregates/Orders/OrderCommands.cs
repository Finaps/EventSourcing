using System;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.CommandBus;

namespace EventSourcing.Example.Domain.Aggregates.Orders;

public record CreateOrder(Guid AggregateId, Guid BasketId) : CommandBase(AggregateId);
    
    
    
public static class OrderCommandHandlers
{
    public static Func<Order, CreateOrder, Order> Create = (order, cmd) =>
    {
        if (order != null)
            throw new ConcurrencyException($"Order with id: {order.Id} already exists");

        order = new Order();
        order.Create(cmd.BasketId);
        return order;
    };
}