namespace EventSourcing.Core.Tests;

public record BankAccountCreatedEvent : Event
{
  public string Name { get; init; }
  public string Iban { get; init; }
}

public abstract record FundsEvent : Event
{
  public decimal Amount { get; init; }
}

public record FundsDepositedEvent : FundsEvent;

public record FundsWithdrawnEvent : FundsEvent;

public record FundsTransferredEvent : FundsEvent
{
  public Guid DebtorAccount { get; init; }
  public Guid CreditorAccount { get; init; }
}

public record BankAccountSnapshot : Snapshot
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

public class BankAccount : Aggregate
{
  public string Name { get; private set; }
  public string Iban { get; private set; }
  public decimal Balance { get; private set; }

  protected override void Apply(Event e)
  {
    switch (e)
    {
      case BankAccountCreatedEvent created:
        Name = created.Name;
        Iban = created.Iban;
        break;
      
      case FundsDepositedEvent deposit:
        Balance += deposit.Amount;
        break;
      case FundsWithdrawnEvent withdraw:
        Balance -= withdraw.Amount;
        break;
      case FundsTransferredEvent transfer:
        if (Id == transfer.DebtorAccount)
          Balance -= transfer.Amount;
        else if (Id == transfer.CreditorAccount)
          Balance += transfer.Amount;
        else
          throw new InvalidOperationException("Not debtor nor creditor of this transaction");
        break;

      case BankAccountSnapshot snapshot:
        Name = snapshot.Name;
        Iban = snapshot.Iban;
        Balance = snapshot.Balance;
        break;
    }

    if (Balance < 0)
      throw new InvalidOperationException("Not enough funds");
  }

  public void Create(string name, string iban) =>
    Apply(new BankAccountCreatedEvent { Name = name, Iban = iban });

  public void Deposit(decimal amount) =>
    Apply(new FundsDepositedEvent { Amount = amount });

  public void Withdraw(decimal amount) =>
    Apply(new FundsWithdrawnEvent { Amount = amount });
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

public abstract partial class AggregateServiceTests
{
  [Fact]
  public async Task Can_Create_And_Persist_BankAccount()
  {
    var account = new BankAccount();
    
    Assert.NotEqual(Guid.Empty, account.Id);
    Assert.Equal(default, account.Name);
    Assert.Equal(default, account.Iban);
    Assert.Equal(default, account.Balance);
    
    account.Apply(new BankAccountCreatedEvent { Name = "E. Vent", Iban = "SOME IBAN" });
    account.Deposit(100);
    
    Assert.Equal("E. Vent", account.Name);
    Assert.Equal("SOME IBAN", account.Iban);
    Assert.Equal(100, account.Balance);
    
    await AggregateService.PersistAsync(account);
  }

  [Fact]
  public async Task Can_Update_BankAccount()
  {
    var account = new BankAccount();
    account.Create("E. Sourcing", "SOME IBAN");
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    var aggregate = await AggregateService.RehydrateAsync<BankAccount>(account.Id);
    aggregate!.Deposit(50);
    await AggregateService.PersistAsync(aggregate);
  }

  [Fact]
  public async Task Can_Update_BankAccount_The_Fancy_Way()
  {
    var account = new BankAccount();
    account.Create("E. Sourcing", "SOME IBAN");
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    await AggregateService.RehydrateAndPersistAsync<BankAccount>(account.Id, x => x.Deposit(50));
  }

  [Fact]
  public async Task Can_Make_BankAccount_Transfer()
  {
    var account = new BankAccount();
    account.Create("E. Sourcing", "SOME IBAN");
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    var anotherAccount = new BankAccount();
    anotherAccount.Create("E. Vent", "SOME OTHER IBAN");

    var transfer = new FundsTransferredEvent
    {
      DebtorAccount = account.Id,
      CreditorAccount = anotherAccount.Id,
      Amount = 20
    };

    account.Apply(transfer);
    anotherAccount.Apply(transfer);

    await AggregateService.PersistAsync(new[] { account, anotherAccount });

    var result1 = await AggregateService.RehydrateAsync<BankAccount>(account.Id);
    var result2 = await AggregateService.RehydrateAsync<BankAccount>(anotherAccount.Id);

    Assert.Equal(80, result1?.Balance);
    Assert.Equal(20, result2?.Balance);
  }

  [Fact]
  public async Task Can_Get_BankAccount_Projections()
  {
    var projections = await RecordStore.GetProjections<BankAccountProjection>()
      .OrderBy(x => x.Name)
      .Take(10)
      .AsAsyncEnumerable()
      .ToListAsync();
    
    Assert.Equal(10, projections.Count);
  }
}