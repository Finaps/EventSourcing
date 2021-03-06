using System.Threading;
using Finaps.EventSourcing.Example.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace Finaps.EventSourcing.Example.Controllers;

// Product requests
public record CreateProduct(string Name, int Quantity);
public record AddStock(int Quantity);

[ApiController]
[Route("[controller]")]
public class ProductsController : Controller
{
    private readonly IAggregateService _aggregateService;

    public ProductsController(IAggregateService aggregateService)
    {
        _aggregateService = aggregateService;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProduct request, CancellationToken cancellationToken = default)
    {
        var product = new Product();
        product.Create(request.Name, request.Quantity);
        await _aggregateService.PersistAsync(product, cancellationToken);
        return product;
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<Product>> GetProduct([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _aggregateService.RehydrateAsync<Product>(id, cancellationToken);
        if(product == null)
            return BadRequest($"Product with id {id} not found");
        
        return product;
    }
    
    [HttpPost("{id:Guid}/addStock")]
    public async Task<ActionResult<Product>> AddStock([FromRoute] Guid id, [FromBody] AddStock request, CancellationToken cancellationToken = default)
    {
        return await _aggregateService.RehydrateAndPersistAsync<Product>(id, 
            product => product.AddStock(request.Quantity), cancellationToken);
    }
}