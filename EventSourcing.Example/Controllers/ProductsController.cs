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
        var product = await _commandBus.ExecuteCommand<Product>(request with {AggregateId = productId});
        return Ok(product.Id);
    }
}