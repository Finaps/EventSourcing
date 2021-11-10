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
        }


        private Func<Basket, CreateBasket, Basket> Create = (basket, _) =>
        {
            if (basket != null)
                throw new ConcurrencyException($"Basket with id: {basket.Id} already exists");

            basket = new Basket();
            basket.Create();
            return basket;
        };
    }
}