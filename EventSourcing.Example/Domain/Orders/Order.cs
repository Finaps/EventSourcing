using System;
using System.Collections.Generic;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Domain.Orders
{
    public class Order : Aggregate
    {
        public Guid BasketId;
        public List<Item> Items = new();
        protected override void Apply<TEvent>(TEvent e)
        {
            switch(e)
            {
                case OrderCreatedEvent createdEvent:
                    BasketId = createdEvent.BasketId;
                    break;
            }
        }
    }
}