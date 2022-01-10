using EventSourcing.Core;
using EventSourcing.Example.CommandBus;
using EventSourcing.Example.Domain.Aggregates.Orders;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : Controller
{
    private readonly ICommandBus _commandBus;
    private readonly IAggregateService _aggregateService;

    public OrdersController(ICommandBus commandBus, IAggregateService aggregateService)
    {
        _commandBus = commandBus;
        _aggregateService = aggregateService;
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<OkObjectResult> GetOrder([FromRoute] Guid id)
    {
        return Ok(await _aggregateService.RehydrateAsync<Order>(id));
    }
}