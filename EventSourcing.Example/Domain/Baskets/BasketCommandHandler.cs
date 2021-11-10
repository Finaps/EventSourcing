using System;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.Commands;

namespace EventSourcing.Example.Domain.Baskets
{
    public class BasketCommandHandler : CommandHandler<Basket>
    {
        public BasketCommandHandler(AggregateService aggregateService) : base(aggregateService)
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