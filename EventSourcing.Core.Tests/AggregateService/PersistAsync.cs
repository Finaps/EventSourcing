namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateService_PersistAsync_Can_Persist_Event()
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
  public async Task AggregateService_PersistAsync_Can_Persist_Multiple_Events()
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
  public async Task AggregateService_PersistAsync_Can_Snapshot_Aggregate()
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
  public async Task AggregateService_PersistAsync_Cannot_Snapshot_When_Interval_Is_Not_Exceeded()
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
  public async Task AggregateService_PersistAsync_One_Snapshot_When_Interval_Is_Exceeded_Twice()
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
  public async Task AggregateService_PersistAsync_Can_Snapshot_Aggregate_When_Appending_One_Event()
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