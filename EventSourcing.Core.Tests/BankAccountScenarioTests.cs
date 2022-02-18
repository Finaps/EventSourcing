using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests;

public record FundsEvent : Event
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

public record BankAccount : Aggregate
{
  public List<FundsEvent> History { get; } = new();
  public decimal Balance { get; private set; }

  public void Deposit(decimal amount) =>
    Add(new FundsDepositedEvent { Amount = amount });

  public void Withdraw(decimal amount) =>
    Add(new FundsWithdrawnEvent { Amount = amount });

  protected override void Apply(Event e)
  {
    switch (e)
    {
      case FundsDepositedEvent deposit:
        Balance += deposit.Amount;
        break;
      case FundsWithdrawnEvent withdraw:
        Balance -= withdraw.Amount;
        break;
      case FundsTransferredEvent transfer:
        if (RecordId == transfer.DebtorAccount)
          Balance -= transfer.Amount;
        else if (RecordId == transfer.CreditorAccount)
          Balance += transfer.Amount;
        else
          throw new InvalidOperationException("Not debtor nor creditor of this transaction");
        break;
    }

    if (Balance < 0)
      throw new InvalidOperationException("Not enough funds");

    if (e is FundsEvent transaction)
      History.Add(transaction);
  }
}

public abstract partial class AggregateServiceTests
{
  [Fact]
  public async Task Can_Create_And_Persist_BankAccount()
  {
    var account = new BankAccount();
    account.Deposit(100);
    await AggregateService.PersistAsync(account);
  }

  [Fact]
  public async Task Can_Update_BankAccount()
  {
    var account = new BankAccount();
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    var aggregate = await AggregateService.RehydrateAsync<BankAccount>(account.RecordId);
    aggregate!.Deposit(50);
    await AggregateService.PersistAsync(aggregate);
  }

  [Fact]
  public async Task Can_Update_BankAccount_The_Fancy_Way()
  {
    var account = new BankAccount();
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    await AggregateService.RehydrateAndPersistAsync<BankAccount>(account.RecordId, x => x.Deposit(50));
  }

  [Fact]
  public async Task Can_Make_BankAccount_Transfer()
  {
    var account = new BankAccount();
    account.Deposit(100);
    await AggregateService.PersistAsync(account);

    var anotherAccount = new BankAccount();

    var transfer = new FundsTransferredEvent
    {
      DebtorAccount = account.RecordId,
      CreditorAccount = anotherAccount.RecordId,
      Amount = 20
    };

    account.Add(transfer);
    anotherAccount.Add(transfer);

    await AggregateService.PersistAsync(new[] { account, anotherAccount });

    var result1 = await AggregateService.RehydrateAsync<BankAccount>(account.RecordId);
    var result2 = await AggregateService.RehydrateAsync<BankAccount>(anotherAccount.RecordId);

    Assert.Equal(80, result1?.Balance);
    Assert.Equal(20, result2?.Balance);
  }
}