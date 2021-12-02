using System.Threading.Tasks;
using EventSourcing.Example.CommandHandler;
using EventSourcing.Example.Domain.Aggregates.Baskets;
using EventSourcing.Example.Domain.Aggregates.Products;
using EventSourcing.Example.EventPublishing;

namespace EventSourcing.Example.Domain.ProcessFlowing
{
    public class ProductReservedEventHandler : IEventHandler<ProductReservedEvent>
    {
        private readonly ICommandHandler<Product> _productCommandHandler;
        public ProductReservedEventHandler(ICommandHandler<Product> productCommandHandler)
        {
            _productCommandHandler = productCommandHandler;
        }
        public async Task Handle(ProductReservedEvent @event)
        {
            var command = new AddProductToBasket(@event.BasketId, @event.AggregateId, @event.Quantity);
            await _productCommandHandler.ExecuteCommand(command);
        }
    }
}