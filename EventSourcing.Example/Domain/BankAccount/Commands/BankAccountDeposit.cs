using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Commands
{
  public class BankAccountDeposit : IBankAccountDeposit
  {
    public decimal Amount { get; init; }
  }
}