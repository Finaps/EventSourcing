using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class AggregateServiceTests
{
  protected abstract IEventStore EventStore { get; }
  protected abstract ISnapshotStore SnapshotStore { get; }
  protected abstract IAggregateService AggregateService { get; }

  [Fact]
  public async Task Can_Persist_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);

    var result = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    Assert.NotNull(result);
  }

  [Fact]
  public Task Cannot_Add_Event_With_Empty_Aggregate_Id()
  {
    var aggregate = new SimpleAggregate { Id = Guid.Empty };
    Assert.Throws<RecordValidationException>(() => aggregate.Add(new EmptyEvent()));
    
    return Task.CompletedTask;
  }

  [Fact]
  public async Task Can_Persist_Multiple_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent())
    };

    await AggregateService.PersistAsync(aggregate);

    var eventCount = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count, eventCount);
  }

  [Fact]
  public async Task Can_Rehydrate_Aggregate()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent())
    };

    await AggregateService.PersistAsync(aggregate);

    var rehydrated = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

    Assert.Equal(events.Count, rehydrated?.Counter);
  }
  
  [Fact]
  public async Task Can_Rehydrate_Aggregate_Up_To_Date()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Add(new EmptyEvent());
    aggregate.Add(new EmptyEvent());
    aggregate.Add(new EmptyEvent { Timestamp = DateTimeOffset.Now.AddYears(1) });

    await AggregateService.PersistAsync(aggregate);

    var rehydrated = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id, DateTimeOffset.Now);

    Assert.Equal(2, rehydrated?.Counter);
  }
    
  [Fact]
  public async Task Rehydrating_Aggregate_Returns_Null_When_No_Events_Are_Found()
  {
    Assert.Null(await AggregateService.RehydrateAsync<EmptyAggregate>(Guid.NewGuid()));
  }

  [Fact]
  public async Task Uncommitted_Events_Are_Cleared_After_Persist()
  {
    var aggregate = new SimpleAggregate();

    aggregate.Add(new EmptyEvent());
    aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);

    Assert.Empty(aggregate.UncommittedEvents);
  }

  [Fact]
  public async Task Empty_Uncommitted_Events_After_Rehydrate()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Add(new EmptyEvent());
    aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);

    var result = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

    Assert.NotNull(result);
    Assert.Empty(result!.UncommittedEvents);
  }

  [Fact]
  public async Task Can_Rehydrate_And_Persist()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
    };

    await AggregateService.PersistAsync(aggregate);
    await AggregateService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
      a => a.Add(new EmptyEvent()));

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }
  
  [Fact]
  public async Task Can_Rehydrate_And_Persist_With_PartitionId()
  {
    var aggregate = new SimpleAggregate { PartitionId = Guid.NewGuid() };
    var events = new List<Event>
    {
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
    };

    await AggregateService.PersistAsync(aggregate);
    await AggregateService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.PartitionId, aggregate.Id,
      a => a.Add(new EmptyEvent()));

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }

  [Fact]
  public async Task Can_Snapshot_Aggregate()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = aggregate.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotResult = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    var eventCount = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(snapshotResult);
    Assert.Equal((int) aggregate.SnapshotInterval, eventCount);
  }
    
  [Fact]
  public async Task Cannot_Snapshot_When_Interval_Is_Not_Exceeded()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = aggregate.SnapshotInterval - 1;
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotCount = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(0, snapshotCount);
    Assert.Equal((int) eventsCount, eventCount);
  }
    
  [Fact]
  public async Task One_Snapshot_When_Interval_Is_Exceeded_Twice()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = 2 * aggregate.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);
    
    var snapshotCount = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.Equal(1, snapshotCount);
    Assert.Equal((int) eventsCount, eventCount);
  }
    
  [Fact]
  public async Task Can_Rehydrate_Aggregate_With_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = aggregate.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());

    await AggregateService.PersistAsync(aggregate);
    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
      
    Assert.NotNull(result);
    Assert.Equal((int) eventsCount, result?.Counter);
    Assert.Equal(0, result?.EventsAppliedAfterHydration);
    Assert.Equal(1, result?.SnapshotsAppliedAfterHydration);
  }
  
  [Fact]
  public async Task Can_Rehydrate_Aggregate_Up_To_Date_With_Snapshots()
  {
    var aggregate = new SnapshotAggregate();

    foreach (var _ in new int[aggregate.SnapshotInterval])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
    
    foreach (var _ in new int[3])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
    
    await Task.Delay(100);
    var date = DateTimeOffset.Now;
    await Task.Delay(100);

    foreach (var _ in new int[aggregate.SnapshotInterval])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);

    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id, date);
    
    var snapshotCount = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var eventsCount = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.NotNull(result);
    Assert.Equal(aggregate.SnapshotInterval + 3, result!.Counter);
    Assert.Equal(3, result.EventsAppliedAfterHydration);
    Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    Assert.Equal(2, snapshotCount);
    Assert.Equal(2 * aggregate.SnapshotInterval + 3, eventsCount);
  }
    
  [Fact]
  public async Task Can_Rehydrate_Aggregate_With_Multiple_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = aggregate.SnapshotInterval;
      
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
      
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
    
    foreach (var _ in new int[eventsCount - 1])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
      
    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
    
    var snapshotCount = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(result);
    Assert.Equal(3 * eventsCount - 1, result!.Counter);
    Assert.Equal(eventsCount - 1, result.EventsAppliedAfterHydration);
    Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    Assert.Equal(2, snapshotCount);
  }
    
  [Fact]
  public async Task Can_Rehydrate_From_Snapshot_And_Events()
  {
    var aggregate = new SnapshotAggregate();
    var eventsCount = aggregate.SnapshotInterval;
      
    foreach (var _ in new int[eventsCount])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
      
    foreach (var _ in new int[eventsCount - 1])
      aggregate.Add(new EmptyEvent());
    await AggregateService.PersistAsync(aggregate);
      
    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);

    var snapshotCount = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(result);
    Assert.Equal(2 * (int) eventsCount - 1, result!.Counter);
    Assert.Equal((int) eventsCount - 1, result.EventsAppliedAfterHydration);
    Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    Assert.Equal(1, snapshotCount);
  }

  [Fact]
  public async Task Can_Persist_Aggregates_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate1 = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate1.Add(new EmptyEvent());

    transaction.Add(aggregate1);

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Add(new EmptyEvent());
    
    transaction.Add(aggregate2);

    await transaction.CommitAsync();

    var result1 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate1.Id);
    Assert.Equal(3, result1?.Counter);

    var result2 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate2.Id);
    Assert.Equal(4, result2?.Counter);
  }
  
  [Fact]
  public async Task Cannot_Persist_Aggregates_In_Transaction_With_Conflicting_Event()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate1 = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate1.Add(new EmptyEvent());

    transaction.Add(aggregate1);

    var aggregate2 = new SimpleAggregate();
    foreach (var _ in new int[4])
      aggregate2.Add(new EmptyEvent());
    
    transaction.Add(aggregate2);

    // Sneakily commit first event of first aggregate before committing transaction
    await EventStore.AddAsync(new List<Event> { aggregate1.UncommittedEvents.First() });

    await Assert.ThrowsAsync<EventStoreException>(async () => await transaction.CommitAsync());

    // Since we manually committed the first event of aggregate1, we still expect one here
    var result1 = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate1.Id);
    Assert.Equal(1, result1?.Counter);

    // aggregate2 should not have been committed
    Assert.Null(await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate2.Id));
  }
  
  [Fact]
  public async Task Cannot_Persist_Aggregates_With_Multiple_PartitionIds_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction(Guid.NewGuid());
    
    var aggregate = new SimpleAggregate { PartitionId = Guid.NewGuid() };
    foreach (var _ in new int[3])
      aggregate.Add(new EmptyEvent());

    Assert.Throws<RecordValidationException>(() => transaction.Add(aggregate));

    await transaction.CommitAsync();

    // aggregate should not have been committed
    Assert.Null(await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id));
  }
  
  [Fact]
  public async Task Cannot_Add_Aggregate_Twice_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate.Add(new EmptyEvent());

    transaction.Add(aggregate);

    Assert.Throws<ArgumentException>(() => transaction.Add(aggregate));
  }
}