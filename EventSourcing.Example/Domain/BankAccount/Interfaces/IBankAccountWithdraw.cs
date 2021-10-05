namespace EventSourcing.Example.Domain.BankAccount.Interfaces
{
  public interface IBankAccountWithdraw
  {
    decimal Amount { get; init; }
  }
}