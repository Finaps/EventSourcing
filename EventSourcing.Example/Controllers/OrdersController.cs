using EventSourcing.Core;
using System;
using System.Threading.Tasks;
using EventSourcing.Example.Domain.Orders;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : Controller
{
    private readonly IAggregateService _aggregateService;

    public OrdersController(IAggregateService aggregateService)
    {
        _aggregateService = aggregateService;
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<Order>> GetOrder([FromRoute] Guid id)
    {
        return await _aggregateService.RehydrateAsync<Order>(id);
    }
}