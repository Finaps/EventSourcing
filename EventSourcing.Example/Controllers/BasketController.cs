using System;
using EventSourcing.Example.CommandHandler;
using EventSourcing.Example.Domain.Aggregates.Baskets;
using EventSourcing.Example.Domain.Aggregates.Orders;
using EventSourcing.Example.Domain.Aggregates.Products;
using EventSourcing.Example.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers
{
    public class BasketController : ControllerBase
    {
        private readonly ICommandHandler<Basket> _basketCommandHandler;
        private readonly ICommandHandler<Order> _productCommandHandler;
        public BasketController(ICommandHandler<Basket> basketCommandHandler, ICommandHandler<Order> productCommandHandler)
        {
            _basketCommandHandler = basketCommandHandler;
            _productCommandHandler = productCommandHandler;
        }
        [HttpPost]
        public OkObjectResult CreateBasket()
        {
            var basketId = Guid.NewGuid();
            _basketCommandHandler.ExecuteCommand(new CreateBasket(basketId));
            return Ok(basketId);
        }
        
        [HttpPost("{basketId:guid}/{productId:guid}/{amount:int}")]
        public OkObjectResult AddItemToBasket([FromRoute] Guid basketId,[FromRoute] Guid productId,[FromRoute] int amount)
        {
            _productCommandHandler.ExecuteCommand(new Reserve(productId ,basketId, amount, Constants.ProductReservationExpires));
            return Ok(true);
        }
    }
}