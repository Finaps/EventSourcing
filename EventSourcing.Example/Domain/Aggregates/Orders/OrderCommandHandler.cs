using System;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Aggregates.Orders
{
    public class OrderCommandHandler : CommandHandler<Order>
    {
        public OrderCommandHandler(IAggregateService aggregateService) : base(aggregateService)
        {
            RegisterCommandHandler(Create);
        }


        private Func<Order, CreateOrder, Order> Create = (order, cmd) =>
        {
            if (order != null)
                throw new ConcurrencyException($"Order with id: {order.Id} already exists");

            order = new Order();
            order.Create(cmd.BasketId);
            return order;
        };
    }
}