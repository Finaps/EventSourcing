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

namespace EventSourcing.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class BasketsController : Controller
{
    private readonly ICommandBus _commandBus;
    private readonly IAggregateService _aggregateService;
    public BasketsController(ICommandBus commandBus, IAggregateService aggregateService)
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
    
    [HttpGet("{basketId:guid}")]
    public async Task<OkObjectResult> GetBasket([FromRoute] Guid basketId)
    {
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        return Ok(basket);
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
        var transaction = _aggregateService.CreateTransaction();
        
        foreach (var item in basket.Items)
        {
            var product = await _commandBus.ExecuteCommand<Product>(new Purchase(item.ProductId, basketId,
                item.Quantity));
            await transaction.PersistAsync(product);
        }
        await _commandBus.ExecuteCommand<Basket>(new CheckoutBasket(basketId));
        var order = await _commandBus.ExecuteCommand<Order>(new CreateOrder(Guid.NewGuid(), basketId));
        
        await transaction.PersistAsync(basket);
        await transaction.PersistAsync(order);
        await transaction.CommitAsync();
        
        return Ok(order.Id);
    }
}