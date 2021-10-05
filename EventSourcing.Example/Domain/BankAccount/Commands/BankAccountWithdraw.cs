using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Commands
{
  public class BankAccountWithdraw : IBankAccountWithdraw
  {
    public decimal Amount { get; init; }
  }
}