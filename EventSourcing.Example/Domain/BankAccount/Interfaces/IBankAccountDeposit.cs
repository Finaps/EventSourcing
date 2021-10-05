namespace EventSourcing.Example.Domain.BankAccount.Interfaces
{
  public interface IBankAccountDeposit
  {
    decimal Amount { get; init; }
  }
}