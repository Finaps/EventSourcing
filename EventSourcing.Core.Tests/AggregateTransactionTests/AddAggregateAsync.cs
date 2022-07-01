namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateTransaction_Can_Persist_Aggregates_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate1 = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate1.Apply(new SimpleEvent());

    await transaction.AddAggregateAsync(aggregate1);

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Apply(new SimpleEvent());
    
    await transaction.AddAggregateAsync(aggregate2);

    await transaction.CommitAsync();

    var result1 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate1.Id);
    Assert.Equal(3, result1?.Counter);

    var result2 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate2.Id);
    Assert.Equal(4, result2?.Counter);
  }
  
  [Fact]
  public async Task AggregateTransaction_Cannot_Persist_Aggregates_In_Transaction_With_Conflicting_Event()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate1 = new SimpleAggregate();
    var e = aggregate1.Apply(new SimpleEvent());
    aggregate1.Apply(new SimpleEvent());
    aggregate1.Apply(new SimpleEvent());
    
    await transaction.AddAggregateAsync(aggregate1);

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Apply(new SimpleEvent());
    
    await transaction.AddAggregateAsync(aggregate2);

    // Sneakily commit first event of first aggregate before committing transaction
    await RecordStore.AddEventsAsync(new List<Event<SimpleAggregate>> { e });

    await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());

    // Since we manually committed the first event of aggregate1, we still expect one here
    var result1 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate1.Id);
    Assert.Equal(1, result1?.Counter);

    // aggregate2 should not have been committed
    Assert.Null(await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate2.Id));
  }
  
  [Fact]
  public async Task AggregateTransaction_Cannot_Add_Aggregates_With_Multiple_PartitionIds_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction(Guid.NewGuid());
    
    var aggregate = new SimpleAggregate { PartitionId = Guid.NewGuid() };
    foreach (var _ in new int[3])
      aggregate.Apply(new SimpleEvent());

    await Assert.ThrowsAsync<RecordValidationException>(async () => await transaction.AddAggregateAsync(aggregate));

    await transaction.CommitAsync();

    // aggregate should not have been committed
    Assert.Null(await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id));
  }
  
  [Fact]
  public async Task AggregateTransaction_Cannot_Add_Aggregate_Twice_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate.Apply(new SimpleEvent());

    await transaction.AddAggregateAsync(aggregate);

    await Assert.ThrowsAsync<ArgumentException>(async () => await transaction.AddAggregateAsync(aggregate));
  }

  [Fact]
  public async Task AggregateTransaction_AddAggregateAsync_Override_Methods_Are_Called()
  {
    var aggregate = new BankAccount();
    aggregate.Apply(new BankAccountCreatedEvent("I. Ban", "NP37BANK012345678910"));

    foreach (var _ in new int[10])
      aggregate.Apply(new BankAccountFundsDepositedEvent(10));

    var transaction = new MockAggregateTransactionSubclass(RecordStore.CreateTransaction());
    await transaction.AddAggregateAsync(aggregate);
    await transaction.CommitAsync();
    
    Assert.Equal(1, transaction.AddAggregateAsyncCallCount);
    Assert.Equal(1, transaction.AddEventsAsyncCallCount);
    Assert.Equal(1, transaction.AddSnapshotAsyncCallCount);
    Assert.Equal(1, transaction.UpsertProjectionAsyncCallCount);
    Assert.Equal(1, transaction.CommitAsyncCallCount);
  }
}