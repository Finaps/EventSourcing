namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_Aggregate()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent())
    };

    await GetAggregateService().PersistAsync(aggregate);

    var rehydrated = await GetAggregateService().RehydrateAsync<SimpleAggregate>(aggregate.Id);

    Assert.Equal(events.Count, rehydrated?.Counter);
  }

  [Fact]
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_Aggregate_Up_To_Date()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());
    aggregate.Apply(new SimpleEvent());
    aggregate.Apply(new SimpleEvent { Timestamp = DateTimeOffset.UtcNow.AddYears(1) });

    await GetAggregateService().PersistAsync(aggregate);

    var rehydrated = await GetAggregateService().RehydrateAsync<SimpleAggregate>(aggregate.Id, DateTimeOffset.UtcNow);

    Assert.Equal(2, rehydrated?.Counter);
  }

  [Fact]
  public async Task AggregateService_RehydrateAsync_Rehydrating_Aggregate_Returns_Null_When_No_Events_Are_Found()
  {
    Assert.Null(await GetAggregateService().RehydrateAsync<EmptyAggregate>(Guid.NewGuid()));
  }

  [Fact]
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_Aggregate_With_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();

    var eventsCount = factory.SnapshotInterval;
    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());

    await GetAggregateService().PersistAsync(aggregate);
    var result = await GetAggregateService().RehydrateAsync<SnapshotAggregate>(aggregate.Id);

    Assert.NotNull(result);
    Assert.Equal((int)eventsCount, result?.Counter);
    Assert.Equal(0, result?.EventsAppliedAfterHydration);
    Assert.Equal(1, result?.SnapshotsAppliedAfterHydration);
  }

  [Fact]
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_Aggregate_Up_To_Date_With_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();

    foreach (var _ in new int[factory.SnapshotInterval])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    foreach (var _ in new int[3])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    await Task.Delay(100);
    var date = DateTimeOffset.UtcNow;
    await Task.Delay(100);

    foreach (var _ in new int[factory.SnapshotInterval])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    var result = await GetAggregateService().RehydrateAsync<SnapshotAggregate>(aggregate.Id, date);

    var snapshotCount = await GetRecordStore()
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var eventsCount = await GetRecordStore()
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
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_Aggregate_With_Multiple_Snapshots()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();

    var eventsCount = factory.SnapshotInterval;

    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    foreach (var _ in new int[eventsCount - 1])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    var result = await GetAggregateService().RehydrateAsync<SnapshotAggregate>(aggregate.Id);

    var snapshotCount = await GetRecordStore()
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
  public async Task AggregateService_RehydrateAsync_Can_Rehydrate_From_Snapshot_And_Events()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();

    var eventsCount = factory.SnapshotInterval;

    foreach (var _ in new int[eventsCount])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    foreach (var _ in new int[eventsCount - 1])
      aggregate.Apply(new SnapshotEvent());
    await GetAggregateService().PersistAsync(aggregate);

    var result = await GetAggregateService().RehydrateAsync<SnapshotAggregate>(aggregate.Id);

    var snapshotCount = await GetRecordStore()
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.NotNull(result);
    Assert.Equal(2 * (int)eventsCount - 1, result!.Counter);
    Assert.Equal((int)eventsCount - 1, result.EventsAppliedAfterHydration);
    Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    Assert.Equal(1, snapshotCount);
  }
}