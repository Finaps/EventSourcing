namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateTransaction_Can_Persist_Aggregates_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate1 = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate1.Apply(new SimpleEvent());

    transaction.Add(aggregate1);

    var hash = aggregate1.ComputeHash();

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Apply(new SimpleEvent());
    
    transaction.Add(aggregate2);

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
    
    transaction.Add(aggregate1);

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Apply(new SimpleEvent());
    
    transaction.Add(aggregate2);

    // Sneakily commit first event of first aggregate before committing transaction
    await RecordStore.AddEventsAsync(new List<Event> { e });

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

    Assert.Throws<RecordValidationException>(() => transaction.Add(aggregate));

    await transaction.CommitAsync();

    // aggregate should not have been committed
    Assert.Null(await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id));
  }
  
  [Fact]
  public Task AggregateTransaction_Cannot_Add_Aggregate_Twice_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate.Apply(new SimpleEvent());

    transaction.Add(aggregate);

    Assert.Throws<ArgumentException>(() => transaction.Add(aggregate));
    
    return Task.CompletedTask;
  }
  

}