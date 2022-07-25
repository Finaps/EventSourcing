using System;
using System.Threading.Tasks;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finaps.EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests
{
  [Fact]
  public async Task DbContextTransactionTests_Can_Use_Custom_Transaction()
  {
    var store = (EntityFrameworkRecordStore) GetRecordStore();
    var service = new AggregateService(store);
    
    await using var transaction = await store.Context.Database.BeginTransactionAsync();

    var bankaccount = new BankAccount();
    bankaccount.Apply(new BankAccountCreatedEvent("E. Vent", "Some IBAN"));

    while (bankaccount.Balance < 100)
      bankaccount.Apply(new BankAccountFundsDepositedEvent(10));

    await service.PersistAsync(bankaccount);

    var projection = bankaccount.Project<BankAccountProjection>()! with { AggregateId = Guid.NewGuid() };

    await store.UpsertProjectionAsync(projection);
    
    // After persisting, check events, snapshots and projections have been inserted
    Assert.True(await store.GetEvents<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.True(await store.GetSnapshots<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.True(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.True(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == projection.AggregateId));

    await transaction.RollbackAsync();

    // After rollback, check that all actions have been undone
    Assert.False(await store.GetEvents<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.False(await store.GetSnapshots<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.False(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    Assert.False(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == projection.AggregateId));
  }
  
  [Fact]
  public async Task DbContextTransactionTests_Transaction_Is_Rolled_Back_On_Conflict()
  {
    var store = (EntityFrameworkRecordStore) GetRecordStore();
    var service = new AggregateService(store);
    
    var bankaccount = new BankAccount();
    var recordStoreExceptionOccurred = false;

    try
    {
      await using var transaction = await store.Context.Database.BeginTransactionAsync();

      var @event = bankaccount.Apply(new BankAccountCreatedEvent("E. Vent", "Some IBAN"));

      while (bankaccount.Balance < 100)
        bankaccount.Apply(new BankAccountFundsDepositedEvent(10));

      await service.PersistAsync(bankaccount);

      // After persisting, check events, snapshots and projections have been inserted
      Assert.True(await store.GetEvents<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
      Assert.True(await store.GetSnapshots<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
      Assert.True(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == bankaccount.Id));

      await store.AddEventsAsync(new[] { @event });
      await transaction.CommitAsync();
    }
    catch (RecordStoreException)
    {
      recordStoreExceptionOccurred = true;
    }
    finally
    {
      Assert.True(recordStoreExceptionOccurred);
      
      // After conflict, check that all actions have been undone
      Assert.False(await store.GetEvents<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
      Assert.False(await store.GetSnapshots<BankAccount>().AnyAsync(x => x.AggregateId == bankaccount.Id));
      Assert.False(await store.GetProjections<BankAccountProjection>().AnyAsync(x => x.AggregateId == bankaccount.Id));
    }
  }
}