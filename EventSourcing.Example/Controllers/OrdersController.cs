using Finaps.EventSourcing.Example.Domain.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Finaps.EventSourcing.Example.Controllers;

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
        var order = await _aggregateService.RehydrateAsync<Order>(id);
        if(order == null)
            return BadRequest($"Order with id {id} not found");
        
        return order;
    }
}