using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Baskets;
using EventSourcing.Example.Domain.Orders;
using EventSourcing.Example.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class BasketsController : Controller
{
    private readonly IAggregateService _aggregateService;
    public BasketsController(IAggregateService aggregateService)
    {
        _aggregateService = aggregateService;
    }
    [HttpPost]
    public async Task<ActionResult<Basket>> CreateBasket()
    {
        var basket = new Basket();
        basket.Create();
        return await _aggregateService.PersistAsync(basket);
    }
    
    [HttpGet("{basketId:guid}")]
    public async Task<ActionResult<Basket>> GetBasket([FromRoute] Guid basketId)
    {
        return await _aggregateService.RehydrateAsync<Basket>(basketId);
    }
        
    [HttpPost("{basketId:guid}/addItem")]
    public async Task<ActionResult<Basket>> AddItemToBasket([FromRoute] Guid basketId,[FromBody] AddProductToBasket request)
    {
        var product = await _aggregateService.RehydrateAsync<Product>(request.ProductId);
        product.ReserveProduct(basketId,request.Quantity);
        
        if (!product.Reservations.Any(x => x.BasketId == basketId && x.Quantity >= request.Quantity))
            return BadRequest($"Reservation of product {request.ProductId} failed: Insufficient stock");

        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        basket.AddProduct(request.Quantity, request.ProductId);
        
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
            var product = await _aggregateService.RehydrateAsync<Product>(item.ProductId);
            product.PurchaseProduct(basketId, item.Quantity);
            await transaction.AddAsync(product);
        }
        basket.CheckoutBasket();
        
        var order = new Order();
        order.Create(basketId);
        
        await transaction.AddAsync(basket);
        await transaction.AddAsync(order);
        await transaction.CommitAsync();
        
        return order.Id;
    }
}