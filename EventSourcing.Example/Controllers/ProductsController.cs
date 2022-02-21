using EventSourcing.Example.Domain.Products;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers;

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
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProduct request)
    {
        var product = new Product();
        product.Create(request.Name, request.Quantity);
        return await _aggregateService.PersistAsync(product);
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<Product>> GetProduct([FromRoute] Guid id)
    {
        var product = await _aggregateService.RehydrateAsync<Product>(id);
        if(product == null)
            return BadRequest($"Product with id {id} not found");
        
        return product;
    }
    
    [HttpPost("{id:Guid}/addStock")]
    public async Task<ActionResult<Product>> AddStock([FromRoute] Guid id, [FromBody] AddStock request)
    {
        return await _aggregateService.RehydrateAndPersistAsync<Product>(id, 
            product => product.AddStock(request.Quantity));
    }
}