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
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        if(basket == null)
            return BadRequest($"Basket with id {basketId} not found");
        
        var product = await _aggregateService.RehydrateAsync<Product>(request.ProductId);
        if (product == null)
            return BadRequest($"Product with id {request.ProductId} not found");
        product.ReserveProduct(basketId,request.Quantity);
        if (!product.Reservations.Any(x => x.BasketId == basketId && x.Quantity >= request.Quantity))
            return BadRequest($"Reservation of product {request.ProductId} failed: Insufficient stock");

        basket.AddProduct(request.Quantity, request.ProductId);
        
        await _aggregateService.PersistAsync(new List<Aggregate> { product, basket });

        return basket;
    }
        
    [HttpPost("{basketId:guid}/checkout")]
    public async Task<ActionResult<Order>> CheckoutBasket([FromRoute] Guid basketId)
    {
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        if(basket == null)
            return BadRequest($"Basket with id {basketId} not found");
        if(basket.Items.Count == 0)
            return BadRequest($"Cannot check out basket with id {basketId}: Basket does not contain any items");
        
        var transaction = _aggregateService.CreateTransaction();
        
        foreach (var item in basket.Items)
        {
            var product = await _aggregateService.RehydrateAsync<Product>(item.ProductId);
            if(product == null)
                return BadRequest($"Product with id {item.ProductId} not found");
            product.PurchaseProduct(basketId, item.Quantity);
            transaction.Add(product);
        }
        basket.CheckoutBasket();
        
        var order = new Order();
        order.Create(basketId);
        
        await transaction.Add(basket).Add(order).CommitAsync();
        
        return order;
    }
}