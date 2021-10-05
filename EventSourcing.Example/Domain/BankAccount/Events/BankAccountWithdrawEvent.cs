using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Events
{
  public class BankAccountWithdrawEvent : Event, IBankAccountWithdraw
  {
    public decimal Amount { get; init; }
  }
}