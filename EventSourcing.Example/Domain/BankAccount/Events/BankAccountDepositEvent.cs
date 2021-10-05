using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Events
{
  public class BankAccountDepositEvent : Event, IBankAccountDeposit
  {
    public decimal Amount { get; init; }
  }
}