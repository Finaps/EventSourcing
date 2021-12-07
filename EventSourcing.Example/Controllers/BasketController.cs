using System;
using System.Threading.Tasks;
using EventSourcing.Example.ComandBus;
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
        public BasketController(ICommandBus commandBus)
        {
            _commandBus = commandBus;
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
    }
}