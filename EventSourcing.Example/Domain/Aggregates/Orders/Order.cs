using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Aggregates.Orders
{
    public class Order : Aggregate
    {
        public Guid BasketId;
        protected override void Apply<TEvent>(TEvent e)
        {
            switch(e)
            {
                case OrderCreatedEvent createdEvent:
                    BasketId = createdEvent.BasketId;
                    break;
            }
        }
        
        public void Create(Guid basketId)
        {
            Add(new OrderCreatedEvent(basketId));
        }
    }
}