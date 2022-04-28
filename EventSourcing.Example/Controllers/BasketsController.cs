using System.Linq;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Example.Domain.Baskets;
using Finaps.EventSourcing.Example.Domain.Orders;
using Finaps.EventSourcing.Example.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace Finaps.EventSourcing.Example.Controllers;

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
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        if(basket == null)
            return BadRequest($"Basket with id {basketId} not found");
        
        return basket;
    }
        
    [HttpPost("{basketId:guid}/addItem")]
    public async Task<ActionResult<Basket>> AddItemToBasket([FromRoute] Guid basketId,[FromBody] AddProductToBasket request)
    {
        // Get basket
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        if(basket == null)
            return BadRequest($"Basket with id {basketId} not found");
        
        // Get product
        var product = await _aggregateService.RehydrateAsync<Product>(request.ProductId);
        if (product == null)
            return BadRequest($"Product with id {request.ProductId} not found");
        
        // Try reserve product
        product.ReserveProduct(basketId,request.Quantity);
        if (!product.Reservations.Any(x => x.BasketId == basketId && x.Quantity >= request.Quantity))
            return BadRequest($"Reservation of product {request.ProductId} failed: Insufficient stock");
        
        // Add product to basket
        basket.AddProduct(request.Quantity, request.ProductId);
        
        // Persist the changes for the basket and product, changes will either both succeed or both fail
        await _aggregateService.PersistAsync(new List<Aggregate> { product, basket });

        return basket;
    }
        
    [HttpPost("{basketId:guid}/checkout")]
    public async Task<ActionResult<Order>> CheckoutBasket([FromRoute] Guid basketId)
    {
        // Get basket
        var basket = await _aggregateService.RehydrateAsync<Basket>(basketId);
        if(basket == null)
            return BadRequest($"Basket with id {basketId} not found");
        if(basket.Items.Count == 0)
            return BadRequest($"Cannot check out basket with id {basketId}: Basket does not contain any items");
        
        // Create transaction which will contain all the aggregates that are being changed by this checkout
        var transaction = _aggregateService.CreateTransaction();
        
        // We need to change every product aggregate sitting in the basket
        foreach (var item in basket.Items)
        {
            // Get product corresponding to the basket item
            var product = await _aggregateService.RehydrateAsync<Product>(item.ProductId);
            if(product == null)
                return BadRequest($"Product with id {item.ProductId} not found");
            
            // Try purchase the product
            if(!product.PurchaseProduct(basketId, item.Quantity))
                return BadRequest($"Purchase of product {item.ProductId} failed: Insufficient stock");
            
            // Add the product to the transaction
            transaction.Add(product);
        }
        // Checkout the basket and create an order
        basket.CheckoutBasket();
        var order = new Order();
        order.Create(basketId);
        
        // Add the checked out basket and the newly created order to the transaction which already contains all the 
        // product changes. Persisting will only succeed if every change on every aggregate in the transaction succeeds
        await transaction.Add(basket).Add(order).CommitAsync();
        
        return order;
    }
}