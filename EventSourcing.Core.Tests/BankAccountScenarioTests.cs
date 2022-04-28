namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task Can_Create_And_Persist_BankAccount()
  {
    var account = new BankAccount();
    
    Assert.NotEqual(Guid.Empty, account.Id);
    Assert.Equal(default, account.Name);
    Assert.Equal(default, account.Iban);
    Assert.Equal(default, account.Balance);

    account.Apply(new BankAccountCreatedEvent("E. Vent", "SOME IBAN"));
    account.Apply(new BankAccountFundsDepositedEvent(100));
    
    Assert.Equal("E. Vent", account.Name);
    Assert.Equal("SOME IBAN", account.Iban);
    Assert.Equal(100, account.Balance);
    
    await AggregateService.PersistAsync(account);
  }

  [Fact]
  public async Task Can_Update_BankAccount()
  {
    var account = new BankAccount();
    account.Apply(new BankAccountCreatedEvent("E. Sourcing", "SOME IBAN"));
    account.Apply(new BankAccountFundsDepositedEvent(100));
    await AggregateService.PersistAsync(account);

    var aggregate = await AggregateService.RehydrateAsync<BankAccount>(account.Id);
    aggregate!.Apply(new BankAccountFundsDepositedEvent(50));
    await AggregateService.PersistAsync(aggregate);
  }

  [Fact]
  public async Task Can_Update_BankAccount_The_Fancy_Way()
  {
    var account = new BankAccount();
    account.Apply(new BankAccountCreatedEvent("E. Sourcing", "SOME IBAN"));
    account.Apply(new BankAccountFundsDepositedEvent(100));
    await AggregateService.PersistAsync(account);

    await AggregateService.RehydrateAndPersistAsync<BankAccount>(account.Id, 
      x => x.Apply(new BankAccountFundsDepositedEvent(50)));
  }

  [Fact]
  public async Task Can_Make_BankAccount_Transfer()
  {
    var account = new BankAccount();
    account.Apply(new BankAccountCreatedEvent("E. Sourcing", "SOME IBAN"));
    account.Apply(new BankAccountFundsDepositedEvent(100));
    await AggregateService.PersistAsync(account);

    var anotherAccount = new BankAccount();
    anotherAccount.Apply(new BankAccountCreatedEvent("E. Sourcing", "SOME OTHER IBAN"));

    var transfer = new BankAccountFundsTransferredEvent(20, account.Id, anotherAccount.Id);

    account.Apply(transfer);
    anotherAccount.Apply(transfer);

    await AggregateService.PersistAsync(new[] { account, anotherAccount });

    var result1 = await AggregateService.RehydrateAsync<BankAccount>(account.Id);
    var result2 = await AggregateService.RehydrateAsync<BankAccount>(anotherAccount.Id);

    Assert.Equal(account.Name, result1?.Name);
    Assert.Equal(account.Iban, result1?.Iban);
    Assert.Equal(80, result1?.Balance);
    
    Assert.Equal(anotherAccount.Name, result2?.Name);
    Assert.Equal(anotherAccount.Iban, result2?.Iban);
    Assert.Equal(20, result2?.Balance);
  }
}