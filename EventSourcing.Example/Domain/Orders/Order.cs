using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Orders;

public record Order : Aggregate
{
    public Guid BasketId { get; private set; }
    protected override void Apply(Event e)
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