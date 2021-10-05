using System;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount;
using EventSourcing.Example.Domain.BankAccount.Commands;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers
{
  [ApiController]
  [Route("BankAccounts")]
  public class BankAccountController : ControllerBase
  {
    private readonly IEventStore _store;

    public BankAccountController(IEventStore store)
    {
      _store = store;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] BankAccountCreate request, CancellationToken cancellationToken)
    {
      var aggregate = new BankAccount();
      aggregate.Create(request);
      await _store.PersistAsync(aggregate, cancellationToken);
      return new CreatedAtRouteResult(nameof(GetById), new { id = aggregate.Id });
    }

    [HttpPost("{id}/deposit")]
    public async Task<ActionResult<BankAccount>> Deposit([FromRoute] Guid id, [FromBody] BankAccountDeposit request, CancellationToken cancellationToken) =>
      await _store.RehydrateAndPersistAsync<BankAccount>(id, x => x.Deposit(request), cancellationToken);
    
    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<BankAccount>> Withdraw([FromRoute] Guid id, [FromBody] BankAccountWithdraw request, CancellationToken cancellationToken) =>
      await _store.RehydrateAndPersistAsync<BankAccount>(id, x => x.Withdraw(request), cancellationToken);

    [HttpGet("{id}")]
    public async Task<ActionResult<BankAccount>> GetById([FromRoute] Guid id, CancellationToken cancellationToken) => 
      await _store.RehydrateAsync<BankAccount>(id, cancellationToken);
  }
}