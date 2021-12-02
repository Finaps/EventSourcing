using System;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Aggregates.Products
{

    public class OrderCommandHandler : CommandHandler<Product>
    {
        public OrderCommandHandler(IAggregateService aggregateService) : base(aggregateService)
        {
            RegisterCommandHandler(Create);
            RegisterCommandHandler(Reserve);
            RegisterCommandHandler(Purchase);
            RegisterCommandHandler(RemoveReservation);
            RegisterCommandHandler(AddStock);
        }


        private Func<Product, Create, Product> Create = (product, cmd) =>
        {
            if (product != null)
                throw new ConcurrencyException($"Order with id: {product.Id} already exists");

            product = new Product();
            product.Create(cmd.Name, cmd.Quantity);
            return product;
        };
        
        private Func<Product, Reserve, Product> Reserve = (product, cmd) =>
        {
            if (product == null)
                throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
            product.ReserveProduct(cmd.BasketId, cmd.Quantity);
            return product;
        };
        
        private Func<Product, Reserve, Product> Purchase = (product, cmd) =>
        {
            if (product == null)
                throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
            product.PurchaseProduct(cmd.BasketId, cmd.Quantity);
            return product;
        };
        
        private Func<Product, Reserve, Product> RemoveReservation = (product, cmd) =>
        {
            if (product == null)
                throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
            product.RemoveReservation(cmd.BasketId, cmd.Quantity);
            return product;
        };
        
        private Func<Product, Reserve, Product> AddStock = (product, cmd) =>
        {
            if (product == null)
                throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
            product.AddStock(cmd.Quantity);
            return product;
        };
    }
}