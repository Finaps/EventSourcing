using EventSourcing.EF.SqlAggregate;
using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.EF;

class BankAccountSqlAggregate : SqlAggregate<BankAccountSqlAggregate, BankAccount>
{
  public string? Name { get; init; }
  public string? Iban { get; init; }
  public decimal Amount { get; init; }

  public BankAccountSqlAggregate()
  {
    Apply<BankAccountCreatedEvent>((aggregate, e) => new BankAccountSqlAggregate
    {
      Name = e.Name,
      Iban = e.Iban
    });
    
    Apply<BankAccountFundsDepositedEvent>((aggregate, e) => new BankAccountSqlAggregate
    {
      Amount = aggregate.Amount + e.Amount
    });
    
    Apply<BankAccountFundsWithdrawnEvent>((aggregate, e) => new BankAccountSqlAggregate
    {
      Amount = aggregate.Amount - e.Amount
    });
    
    Apply<BankAccountFundsTransferredEvent>((aggregate, e) => new BankAccountSqlAggregate
    {
      Amount = aggregate.Amount - (aggregate.AggregateId == e.DebtorAccount ? -e.Amount : e.Amount)
    });
  }
}