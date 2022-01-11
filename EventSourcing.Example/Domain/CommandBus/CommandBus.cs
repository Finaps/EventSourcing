using EventSourcing.Core;
using EventSourcing.Example.CommandBus;
using EventSourcing.Example.Domain.Aggregates.Baskets;
using EventSourcing.Example.Domain.Aggregates.Orders;
using EventSourcing.Example.Domain.Aggregates.Products;

namespace EventSourcing.Example.Domain.CommandBus;

public class CommandBus : CommandBusBase
{
    public CommandBus(IAggregateService aggregateService) : base(aggregateService)
    {
        RegisterCommandHandler(BasketCommandHandlers.Create);
        RegisterCommandHandler(BasketCommandHandlers.CheckoutBasket);
        RegisterCommandHandler(BasketCommandHandlers.AddProductToBasket);
        RegisterCommandHandler(BasketCommandHandlers.RemoveProductFromBasket);
        RegisterCommandHandler(ProductCommandHandlers.Create);
        RegisterCommandHandler(ProductCommandHandlers.Purchase);
        RegisterCommandHandler(ProductCommandHandlers.Reserve);
        RegisterCommandHandler(ProductCommandHandlers.AddStock);
        RegisterCommandHandler(ProductCommandHandlers.RemoveReservation);
        RegisterCommandHandler(OrderCommandHandlers.Create);
    }
}