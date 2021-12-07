using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Example.CommandBus;
using EventSourcing.Example.Domain.Aggregates.Baskets;
using EventSourcing.Example.Domain.Aggregates.Orders;
using EventSourcing.Example.Domain.Aggregates.Products;
using EventSourcing.Example.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers
{
    public class BasketController : ControllerBase
    {
        private readonly ICommandBus _commandBus;
        private readonly IAggregateService<Event> _aggregateService;
        public BasketController(ICommandBus commandBus, IAggregateService<Event> aggregateService)
        {
            _commandBus = commandBus;
            _aggregateService = aggregateService;
        }
        [HttpPost]
        public async Task<OkObjectResult> CreateBasket()
        {
            var basketId = Guid.NewGuid();
            var basket = await _commandBus.ExecuteCommand<Basket>(new CreateBasket(basketId));
            return Ok(basket.Id);
        }
        
        [HttpPost("{basketId:guid}/{productId:guid}/{amount:int}")]
        public async Task<ObjectResult> AddItemToBasket([FromRoute] Guid basketId,[FromRoute] Guid productId,[FromRoute] int amount)
        {
            var product = await _commandBus.ExecuteCommand<Product>(new Reserve(productId ,basketId, amount, Constants.ProductReservationExpires));
            
            if (product.Reservations.Find(x => x.BasketId == basketId && x.Quantity >= amount) == null)
                return BadRequest($"Reservation of product {productId} failed");

            var basket = await _commandBus.ExecuteCommand<Basket>(new AddProductToBasket(basketId, productId, amount));
            
            return Ok(basket);
        }
        
        [HttpPost("{basketId:guid}/checkout")]
        public async Task<ObjectResult> CheckoutBasket([FromRoute] Guid basketId)
        {
            var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
            var successfullyCheckedOut = new List<Item>();

            try
            {
                foreach (var item in basket.Items)
                {
                    await _commandBus.ExecuteCommand<Product>(new Purchase(item.ProductId, basketId,
                            item.Quantity));
                    successfullyCheckedOut.Add(item);
                }

                await _commandBus.ExecuteCommand<Basket>(new CheckoutBasket(basketId));
            }
            catch (Exception e)
            {
                // Rollback of all product purchases that were successful
                foreach(var item in successfullyCheckedOut)
                    await _commandBus.ExecuteCommand<Product>(new AddStock(item.ProductId, item.Quantity));
                return BadRequest(e);
            }
            
            var order = await _commandBus.ExecuteCommand<Order>(new CreateOrder(Guid.NewGuid(), basketId));
            return Ok(order);
        }
    }
}