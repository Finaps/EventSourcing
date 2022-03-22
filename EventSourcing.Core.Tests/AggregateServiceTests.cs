namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{

  [Fact]
  public async Task Can_Persist_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());

    await AggregateService.PersistAsync(aggregate);

    var result = await RecordStore
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    Assert.NotNull(result);
  }

  [Fact]
  public Task Cannot_Add_Event_With_Empty_Aggregate_Id()
  {
    var aggregate = new SimpleAggregate { Id = Guid.Empty };
    Assert.Throws<RecordValidationException>(() => aggregate.Apply(new SimpleEvent()));
    
    return Task.CompletedTask;
  }

  [Fact]
  public async Task Can_Persist_Multiple_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent())
    };

    await AggregateService.PersistAsync(aggregate);

    var eventCount = await RecordStore
      .GetEvents<SimpleAggregate>()
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
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent())
    };

    await AggregateService.PersistAsync(aggregate);

    var rehydrated = await AggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

    Assert.Equal(events.Count, rehydrated?.Counter);
  }
  
  [Fact]
  public async Task Can_Rehydrate_Aggregate_Up_To_Date()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());
    aggregate.Apply(new SimpleEvent());
    aggregate.Apply(new SimpleEvent { Timestamp = DateTimeOffset.Now.AddYears(1) });

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
  public async Task Can_Rehydrate_And_Persist()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
    };

    await AggregateService.PersistAsync(aggregate);
    await AggregateService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
      a => a.Apply(new SimpleEvent()));

    var count = await RecordStore
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }

  [Fact]
  public async Task Can_Rehydrate_And_Persist_MockAggregate()
  {
    var aggregate = new MockAggregate();
    var e = aggregate.Apply(new MockEvent
    {
      MockBoolean = true,
      MockString = "Hello World",
      MockDecimal = 0.55m,
      MockDouble = 0.6,
      MockEnum = MockEnum.C,
      MockFlagEnum = MockFlagEnum.A | MockFlagEnum.D,

      MockNestedRecord = new MockNestedRecord
      {
        MockBoolean = false,
        MockString = "Bonjour",
        MockDecimal = 0.82m,
        MockDouble = 0.999
      },

      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new()
        {
          MockBoolean = false,
          MockString = "Bye Bye",
          MockDecimal = 99.99m,
          MockDouble = 0.111
        },
      },

      MockFloatList = new List<float> { .1f, .5f, .9f },
      MockStringSet = new List<string> { "A", "B", "C", "C" }
    });

    await AggregateService.PersistAsync(aggregate);

    var result = await AggregateService.RehydrateAsync<MockAggregate>(aggregate.Id);
    
    Assert.Equal(e.MockBoolean, result.MockBoolean);
    Assert.Equal(e.MockString, result.MockString);
    Assert.Equal(e.MockDecimal, result.MockDecimal);
    Assert.Equal(e.MockDouble, result.MockDouble);
    Assert.Equal(e.MockEnum, result.MockEnum);
    Assert.Equal(e.MockFlagEnum, result.MockFlagEnum);
    Assert.Equal(e.MockNestedRecord.MockBoolean, result.MockNestedRecord.MockBoolean);
    Assert.Equal(e.MockNestedRecord.MockString, result.MockNestedRecord.MockString);
    Assert.Equal(e.MockNestedRecord.MockDecimal, result.MockNestedRecord.MockDecimal);
    Assert.Equal(e.MockNestedRecord.MockDouble, result.MockNestedRecord.MockDouble);
    Assert.Equal(e.MockNestedRecordList.Single().MockBoolean, result.MockNestedRecordList.Single().MockBoolean);
    Assert.Equal(e.MockNestedRecordList.Single().MockString, result.MockNestedRecordList.Single().MockString);
    Assert.Equal(e.MockNestedRecordList.Single().MockDecimal, result.MockNestedRecordList.Single().MockDecimal);
    Assert.Equal(e.MockNestedRecordList.Single().MockDouble, result.MockNestedRecordList.Single().MockDouble);
    Assert.Equal(e.MockFloatList[0], result.MockFloatList[0]);
    Assert.Equal(e.MockFloatList[1], result.MockFloatList[1]);
    Assert.Equal(e.MockFloatList[2], result.MockFloatList[2]);
    Assert.Contains(e.MockStringSet, x => x == "A");
    Assert.Contains(e.MockStringSet, x => x == "B");
    Assert.Contains(e.MockStringSet, x => x == "C");
  }
  
  [Fact]
  public async Task Can_Rehydrate_And_Persist_With_PartitionId()
  {
    var aggregate = new SimpleAggregate { PartitionId = Guid.NewGuid() };
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
    };

    await AggregateService.PersistAsync(aggregate);
    await AggregateService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.PartitionId, aggregate.Id,
      a => a.Apply(new SimpleEvent()));

    var count = await RecordStore
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }

  [Fact]
  public async Task Can_Snapshot_Aggregate()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotResult = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(snapshotResult);
    Assert.Equal((int) factory.SnapshotInterval, eventCount);
  }
    
  [Fact]
  public async Task Cannot_Snapshot_When_Interval_Is_Not_Exceeded()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval - 1;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);

    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .LongCountAsync();

    Assert.Equal(0, snapshotCount);
    Assert.Equal(eventsCount, eventCount);
  }
    
  [Fact]
  public async Task One_Snapshot_When_Interval_Is_Exceeded_Twice()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = 2 * factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await AggregateService.PersistAsync(aggregate);
    
    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .LongCountAsync();
      
    Assert.Equal(1, snapshotCount);
    Assert.Equal(eventsCount, eventCount);
  }
    
  [Fact]
  public async Task Can_Rehydrate_Aggregate_With_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

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
    var factory = new SimpleSnapshotFactory();

    foreach (var _ in new int[factory.SnapshotInterval])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
    
    foreach (var _ in new int[3])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
    
    await Task.Delay(100);
    var date = DateTimeOffset.Now;
    await Task.Delay(100);

    foreach (var _ in new int[factory.SnapshotInterval])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);

    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id, date);
    
    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var eventsCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.NotNull(result);
    Assert.Equal(factory.SnapshotInterval + 3, result!.Counter);
    Assert.Equal(3, result.EventsAppliedAfterHydration);
    Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    Assert.Equal(2, snapshotCount);
    Assert.Equal(2 * factory.SnapshotInterval + 3, eventsCount);
  }
    
  [Fact]
  public async Task Can_Rehydrate_Aggregate_With_Multiple_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
      
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
      
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
    
    foreach (var _ in new int[eventsCount - 1])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
      
    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
    
    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
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
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
      
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
      
    foreach (var _ in new int[eventsCount - 1])
      aggregate.Apply(new SnapshotEvent());
    await AggregateService.PersistAsync(aggregate);
      
    var result = await AggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);

    var snapshotCount = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
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
  public async Task Cannot_Persist_Aggregates_In_Transaction_With_Conflicting_Event()
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
  public async Task Cannot_Persist_Aggregates_With_Multiple_PartitionIds_In_Transaction()
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
  public Task Cannot_Add_Aggregate_Twice_In_Transaction()
  {
    var transaction = AggregateService.CreateTransaction();
    
    var aggregate = new SimpleAggregate();
    foreach (var _ in new int[3])
      aggregate.Apply(new SimpleEvent());

    transaction.Add(aggregate);

    Assert.Throws<ArgumentException>(() => transaction.Add(aggregate));
    
    return Task.CompletedTask;
  }
  
  [Fact]
  public async Task Can_Snapshot_Aggregate_When_Appending_One_Event()
  {
    // To test for an previous issue where snapshotting was not happening when exactly one event was persisted
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();
    
    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
    {
      aggregate.Apply(new SnapshotEvent());
      await AggregateService.PersistAsync(aggregate);
    }

    var snapshotResult = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleOrDefaultAsync();

    var eventCount = await RecordStore
      .GetEvents<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
      
    Assert.NotNull(snapshotResult);
    Assert.Equal((int) factory.SnapshotInterval, eventCount);
  }
}