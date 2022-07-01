namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordTransaction_AddEvents_Can_Add_Event_In_Transaction()
  {
    var e = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };

    await RecordStore.CreateTransaction()
      .AddEvents(new List<Event<EmptyAggregate>> { e })
      .CommitAsync();

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(1, count);
  }
  
  [Fact]
  public async Task RecordTransaction_AddEvents_Can_Add_Multiple_Events_In_Transaction()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent { AggregateId = Guid.NewGuid() });
    var e2 = aggregate.Apply(new EmptyEvent { AggregateId = Guid.NewGuid() });

    await RecordStore.CreateTransaction()
      .AddEvents(new List<Event<EmptyAggregate>> { e1 })
      .AddEvents(new List<Event<EmptyAggregate>> { e2 })
      .CommitAsync();

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == e1.AggregateId || x.AggregateId == e2.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(2, count);
  }

  [Fact]
  public async Task RecordTransaction_AddEvents_Cannot_Add_Different_Partition_Key_In_Transaction()
  {
    var e = new EmptyEvent
    {
      PartitionId = Guid.NewGuid(),
      AggregateId = Guid.NewGuid()
    };

    var transaction = RecordStore.CreateTransaction(Guid.NewGuid());
    Assert.Throws<RecordValidationException>(() => transaction.AddEvents(new List<Event<EmptyAggregate>> { e }));
    await transaction.CommitAsync();
      
    // Ensure e was not committed
    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.PartitionId == e.PartitionId && x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
  
    [Fact]
  public async Task Cannot_Add_Duplicate_Event_In_Transaction()
  {
    var e1 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };
    var e2 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };

    // Commit e1 in an earlier transaction
    await RecordStore.AddEventsAsync(new [] { e1 });

    // Try to commit both e1 & e2
    var transaction = RecordStore.CreateTransaction()
      .AddEvents(new List<Event<EmptyAggregate>> { e1 })
      .AddEvents(new List<Event<EmptyAggregate>> { e2 });

    // Check commit throws a concurrency exception
    await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());

    // Ensure e2 was not committed
    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == e2.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }

  [Fact]
  public async Task Cannot_Add_Events_When_Events_Are_Deleted_Concurrently()
  {
    var store = RecordStore;
    
    var aggregate = new EmptyAggregate();
    
    // Add 5 Events
    await store.AddEventsAsync(Enumerable
      .Range(0, 5)
      .Select(_ => aggregate.Apply(new EmptyEvent()))
      .ToArray());

    // Then, start transaction, adding 5 additional events
    var transaction = store.CreateTransaction()
      .AddEvents(Enumerable
        .Range(0, 5)
        .Select(_ => aggregate.Apply(new EmptyEvent()))
        .Cast<Event<EmptyAggregate>>()
        .ToArray());
    
    // Then delete the first 5 events, simulating concurrency
    await store.DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    // Check if committing transaction throws NonConsecutiveException
    await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());

    // check if events were deleted and transaction did not add additional events
    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
}