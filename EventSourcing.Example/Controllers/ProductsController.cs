using System;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Example.CommandBus;
using EventSourcing.Example.Domain.Aggregates.Products;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController : Controller
{
    private readonly ICommandBus _commandBus;
    private readonly IAggregateService _aggregateService;

    public ProductsController(ICommandBus commandBus, IAggregateService aggregateService)
    {
        _commandBus = commandBus;
        _aggregateService = aggregateService;
    }

    [HttpPost]
    public async Task<OkObjectResult> CreateProduct([FromBody] CreateProduct request)
    {
        var productId = Guid.NewGuid();
        var product = await _commandBus.ExecuteCommandAndSaveChanges<Product>(request with {AggregateId = productId});
        return Ok(product.Id);
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<OkObjectResult> GetProduct([FromRoute] Guid id)
    {
        return Ok(await _aggregateService.RehydrateAsync<Product>(id));
    }
    
    [HttpPost("{id:Guid}/setStock")]
    public async Task<OkObjectResult> SetStock([FromRoute] Guid id, [FromBody] AddStock request)
    {
        return Ok(await _commandBus.ExecuteCommandAndSaveChanges<Product>(request with {AggregateId = id}));
    }
}