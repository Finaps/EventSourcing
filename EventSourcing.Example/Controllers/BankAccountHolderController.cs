using System;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount;
using EventSourcing.Example.Domain.BankAccount.Commands;
using EventSourcing.Example.Domain.BankAccountHolder;
using EventSourcing.Example.Domain.BankAccountHolder.Commands;
using Microsoft.AspNetCore.Mvc;

namespace EventSourcing.Example.Controllers
{
    [ApiController]
    [Route("BankAccountHolders")]
    public class BankAccountHolderController : ControllerBase
    {
        private readonly IEventService _service;

        public BankAccountHolderController(IEventService service)
        {
            _service = service;
        }
        
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BankAccountHolderCreate request, CancellationToken cancellationToken)
        {
            var aggregate = new BankAccountHolder();
            aggregate.Create(request);
            await _service.PersistAsync(aggregate, cancellationToken);
            return new CreatedAtRouteResult(nameof(GetById), new { id = aggregate.Id });
        }
        
        [HttpPost("{id}/update")]
        public async Task<ActionResult<BankAccountHolder>> Update([FromRoute] Guid id, [FromBody] BankAccountHolderUpdate request, CancellationToken cancellationToken) =>
            await _service.RehydrateAndPersistAsync<BankAccountHolder>(id, x => x.Update(request), cancellationToken);

        [HttpPost("{id}/linkBankAccount")]
        public async Task<ActionResult<BankAccountHolder>> LinkBankAccount([FromRoute] Guid id,
            [FromBody] BankAccountHolderAddBankAccount request, CancellationToken cancellationToken)
        {
            var bankAccount = await _service.RehydrateAsync<BankAccount>(request.BankAccountId, cancellationToken);
            if (bankAccount == null) return BadRequest("Bank account does not exist");
            
            bankAccount.LinkOwner(new BankAccountLinkOwner{ Owner = id });
            await _service.PersistAsync(bankAccount, cancellationToken);
            return await _service.RehydrateAndPersistAsync<BankAccountHolder>(id, x => x.AddBankAccount(request), cancellationToken);
        }
        
        [HttpPost("{id}/unlinkBankAccount")]
        public async Task<ActionResult<BankAccountHolder>> UnlinkBankAccount([FromRoute] Guid id, [FromBody] BankAccountHolderRemoveBankAccount request, CancellationToken cancellationToken) =>
            await _service.RehydrateAndPersistAsync<BankAccountHolder>(id, x => x.RemoveBankAccount(request), cancellationToken);

        [HttpGet("{id}")]
        public async Task<ActionResult<BankAccountHolder>> GetById([FromRoute] Guid id, CancellationToken cancellationToken) => 
            await _service.RehydrateAsync<BankAccountHolder>(id, cancellationToken);
    }
}