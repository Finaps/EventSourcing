using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task<ActionResult<Guid>> CreateBasket()
    {
        var basketId = Guid.NewGuid();
        var basket = await _commandBus.ExecuteCommandAndSaveChanges<Basket>(new CreateBasket(basketId));
        return basket.Id;
    }
    
    [HttpGet("{basketId:guid}")]
    public async Task<ActionResult<Basket>> GetBasket([FromRoute] Guid basketId)
    {
        return await _aggregateService.RehydrateAsync<Basket>(basketId);
    }
        
    [HttpPost("{basketId:guid}/addItem")]
    public async Task<ActionResult<Basket>> AddItemToBasket([FromRoute] Guid basketId,[FromBody] AddProductToBasket request)
    {
        var product = await _commandBus.ExecuteCommand<Product>(new Reserve(request.ProductId ,basketId, request.Quantity, Constants.ProductReservationExpires));
        
        if (!product.Reservations.Any(x => x.BasketId == basketId && x.Quantity >= request.Quantity))
            return BadRequest($"Reservation of product {request.ProductId} failed: Insufficient stock");

        var basket = await _commandBus.ExecuteCommand<Basket>(request with {AggregateId = basketId});

        await _aggregateService.PersistAsync(new List<Aggregate> { product, basket });

        return basket;
    }
        
    [HttpPost("{basketId:guid}/checkout")]
    public async Task<ActionResult<Guid>> CheckoutBasket([FromRoute] Guid basketId)
    {
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        var transaction = _aggregateService.CreateTransaction();
        
        foreach (var item in basket.Items)
        {
            var product = await _commandBus.ExecuteCommand<Product>(new Purchase(item.ProductId, basketId,
                item.Quantity));
            await transaction.AddAsync(product);
        }
        await _commandBus.ExecuteCommand<Basket>(new CheckoutBasket(basketId));
        var order = await _commandBus.ExecuteCommand<Order>(new CreateOrder(Guid.NewGuid(), basketId));
        
        await transaction.AddAsync(basket);
        await transaction.AddAsync(order);
        await transaction.CommitAsync();
        
        return order.Id;
    }
}