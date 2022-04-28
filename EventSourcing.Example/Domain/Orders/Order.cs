using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Example.Domain.Orders;

public class Order : Aggregate<Order>
{
    public Guid BasketId { get; private set; }
    protected override void Apply(Event<Order> e)
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
        Apply(new OrderCreatedEvent(basketId));
    }
}