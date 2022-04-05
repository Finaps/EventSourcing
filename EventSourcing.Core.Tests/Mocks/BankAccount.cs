namespace EventSourcing.Core.Tests.Mocks;

public abstract record BankAccountEvent : Event<BankAccount>;

public record BankAccountCreatedEvent : BankAccountEvent
{
  public string Name { get; init; }
  public string Iban { get; init; }
}

public abstract record BankAccountFundsEvent : BankAccountEvent
{
  public decimal Amount { get; init; }
}

public record BankAccountFundsDepositedEvent : BankAccountFundsEvent;

public record BankAccountFundsWithdrawnEvent : BankAccountFundsEvent;

public record BankAccountFundsTransferredEvent : BankAccountFundsEvent
{
  public Guid DebtorAccount { get; init; }
  public Guid CreditorAccount { get; init; }
}

public record BankAccountSnapshot : Snapshot<BankAccount>
{
  public string Name { get; init; }
  public string Iban { get; init; }
  public decimal Balance { get; init; }
}

public record BankAccountProjection : Projection
{
  public string Name { get; init; }
  public string Iban { get; init; }
}

public class BankAccount : Aggregate<BankAccount>
{
  public string Name { get; private set; }
  public string Iban { get; private set; }
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
      case BankAccountFundsTransferredEvent transfer:
        if (Id == transfer.DebtorAccount)
          Balance -= transfer.Amount;
        else if (Id == transfer.CreditorAccount)
          Balance += transfer.Amount;
        else
          throw new InvalidOperationException("Not debtor nor creditor of this transaction");
        break;
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

  public void Create(string name, string iban) =>
    Apply(new BankAccountCreatedEvent { Name = name, Iban = iban });

  public void Deposit(decimal amount) =>
    Apply(new BankAccountFundsDepositedEvent { Amount = amount });

  public void Withdraw(decimal amount) =>
    Apply(new BankAccountFundsWithdrawnEvent { Amount = amount });
}

public class BankAccountSnapshotFactory : SnapshotFactory<BankAccount, BankAccountSnapshot>
{
  public override long SnapshotInterval => 10;

  protected override BankAccountSnapshot CreateSnapshot(BankAccount aggregate) => new BankAccountSnapshot()
  {
    Name = aggregate.Name,
    Iban = aggregate.Iban,
    Balance = aggregate.Balance
  };
}

public class BankAccountProjectionFactory : ProjectionFactory<BankAccount, BankAccountProjection>
{
  protected override BankAccountProjection CreateProjection(BankAccount aggregate) => new BankAccountProjection()
  {
    Name = aggregate.Name.ToUpper(),
    Iban = aggregate.Iban
  };
}