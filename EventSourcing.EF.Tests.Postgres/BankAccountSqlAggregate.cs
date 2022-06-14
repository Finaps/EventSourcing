using EventSourcing.EF.SqlAggregate;

namespace Finaps.EventSourcing.EF;

record BankAccountSqlAggregate : SQLAggregate
{
  public string? Name { get; init; }
  public string? Iban { get; init; }
  public decimal Amount { get; init; }
}