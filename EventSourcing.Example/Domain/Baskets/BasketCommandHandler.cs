using System;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Baskets
{
    public class BasketCommandHandler : CommandHandler<Basket>
    {
        public BasketCommandHandler(IAggregateService aggregateService) : base(aggregateService)
        {
            RegisterCommandHandler(Create);
            RegisterCommandHandler(AddProductToBasket);
            RegisterCommandHandler(RemoveProductFromBasket);
            RegisterCommandHandler(CheckoutBasket);
        }


        private Func<Basket, CreateBasket, Basket> Create = (basket, _) =>
        {
            if (basket != null)
                throw new ConcurrencyException($"Basket with id: {basket.Id} already exists");

            basket = new Basket();
            basket.Create();
            return basket;
        };
        
        private Func<Basket, AddProductToBasket, Basket> AddProductToBasket = (basket, cmd) =>
        {
            if (basket == null)
                throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
            basket.AddProduct(cmd.Quantity, cmd.ProductId);
            return basket;
        };
        
        private Func<Basket, AddProductToBasket, Basket> RemoveProductFromBasket = (basket, cmd) =>
        {
            if (basket == null)
                throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
            basket.RemoveProduct(cmd.Quantity, cmd.ProductId);
            return basket;
        };
        
        private Func<Basket, AddProductToBasket, Basket> CheckoutBasket = (basket, cmd) =>
        {
            if (basket == null)
                throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
            basket.CheckoutBasket();
            return basket;
        };
    }
}