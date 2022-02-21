using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Add(new EmptyEvent());
    var e2 = aggregate.Add(new EmptyEvent());

    e2 = e2 with { Index = 0 };

    await RecordStore.AddEventsAsync(new Event[] { e1 });

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e2 }));
  }
  
  [Fact]
  public async Task Cannot_Add_NonConsecutive_Events()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Add(new EmptyEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e1 });

    var e2 = aggregate.Add(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e2 }));
  }
  
  [Fact]
  public async Task Cannot_Add_Duplicate_Event_In_Transaction()
  {
    var e1 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };
    var e2 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };

    // Commit e1 in an earlier transaction
    await RecordStore.AddEventsAsync(new List<Event> { e1 });

    // Try to commit both e1 & e2
    var transaction = RecordStore.CreateTransaction()
      .AddEvents(new List<Event> { e1 })
      .AddEvents(new List<Event> { e2 });

    // Check commit throws a concurrency exception
    await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());

    // Ensure e2 was not committed
    var count = await RecordStore.Events
      .Where(x => x.AggregateId == e2.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }

  [Fact]
  public async Task Cannot_Add_Events_When_Events_Are_Deleted_Concurrently()
  {
    var aggregate = new EmptyAggregate();
    
    // Add 5 Events
    await RecordStore.AddEventsAsync(Enumerable
      .Range(0, 5)
      .Select(_ => aggregate.Add(new EmptyEvent()))
      .Cast<Event>()
      .ToList());

    // Then, start transaction, adding 5 additional events
    var transaction = RecordStore.CreateTransaction()
      .AddEvents(Enumerable
        .Range(0, 5)
        .Select(_ => aggregate.Add(new EmptyEvent()))
        .Cast<Event>()
        .ToList());
    
    // Then delete the first 5 events, simulating concurrency
    await RecordStore.DeleteEventsAsync(aggregate.Id);

    // Check if committing transaction throws NonConsecutiveException
    await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());

    // check if events were deleted and transaction did not add additional events
    var count = await RecordStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
}