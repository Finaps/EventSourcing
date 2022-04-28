namespace Finaps.EventSourcing.Core.Tests.Mocks;

public record BankAccountCreatedEvent(string Name, string Iban) : Event<BankAccount>;

public abstract record BankAccountFundsEvent(decimal Amount) : Event<BankAccount>;

public record BankAccountFundsDepositedEvent(decimal Amount) : BankAccountFundsEvent(Amount);

public record BankAccountFundsWithdrawnEvent(decimal Amount) : BankAccountFundsEvent(Amount);

public record BankAccountFundsTransferredEvent(decimal Amount, Guid DebtorAccount, Guid CreditorAccount) : BankAccountFundsEvent(Amount);

public record BankAccountSnapshot(string Name, string Iban, decimal Balance) : Snapshot<BankAccount>;

public record BankAccountProjection(string Name, string Iban) : Projection;

public class BankAccount : Aggregate<BankAccount>
{
  public string? Name { get; private set; }
  public string? Iban { get; private set; }
  public decimal Balance { get; private set; }

  protected override void Apply(Event<BankAccount> e)
  {
    switch (e)
    {
      case BankAccountCreatedEvent created:
        Name = created.Name;
        Iban = created.Iban;
        break;
      
      case BankAccountFundsDepositedEvent deposit:
        Balance += deposit.Amount;
        break;
      case BankAccountFundsWithdrawnEvent withdraw:
        Balance -= withdraw.Amount;
        break;
      
      case BankAccountFundsTransferredEvent transfer when transfer.DebtorAccount == Id:
        Balance -= transfer.Amount;
        break;
      case BankAccountFundsTransferredEvent transfer when transfer.CreditorAccount == Id:
        Balance += transfer.Amount;
        break;
      case BankAccountFundsTransferredEvent:
        throw new InvalidOperationException("Not debtor nor creditor of this transaction");
      
      default:
        throw new ArgumentOutOfRangeException(nameof(e));
    }

    if (Balance < 0)
      throw new InvalidOperationException("Not enough funds");
  }

  protected override void Apply(Snapshot<BankAccount> s)
  {
    switch (s)
    {
      case BankAccountSnapshot snapshot:
        Name = snapshot.Name;
        Iban = snapshot.Iban;
        Balance = snapshot.Balance;
        break;
    }
  }
}

public class BankAccountSnapshotFactory : SnapshotFactory<BankAccount, BankAccountSnapshot>
{
  public override long SnapshotInterval => 10;

  protected override BankAccountSnapshot CreateSnapshot(BankAccount aggregate) =>
    new (Name: aggregate.Name!, Iban: aggregate.Iban!, Balance: aggregate.Balance);
}

public class BankAccountProjectionFactory : ProjectionFactory<BankAccount, BankAccountProjection>
{
  protected override BankAccountProjection CreateProjection(BankAccount aggregate) =>
    new (aggregate.Name?.ToUpper()!, aggregate.Iban!);
}