using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteAggregateAsync_Can_Delete_Events_With_DeleteAggregateAll()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 3; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);

    var deleted = await RecordStore.DeleteAggregateAsync<EmptyAggregate>(Guid.Empty, aggregate.Id);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(3, deleted);
    Assert.Equal(0, count);
  }

  [Fact]
  public async Task RecordStore_DeleteAggregateAsync_Can_Delete_All()
  {
    var aggregate = new EmptyAggregate();

    // Store event
    var e = aggregate.Apply(new EmptyEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });
    Assert.NotNull(await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    // Store snapshot
    var snapshot = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate) };
    await RecordStore.AddSnapshotAsync(snapshot);
    Assert.NotNull(await RecordStore
      .GetSnapshots<EmptyAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    // Store projection
    var projection = new EmptyProjection
      { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Hash = "RANDOM" };
    await RecordStore.UpsertProjectionAsync(projection);
    Assert.NotNull(await RecordStore.GetProjectionByIdAsync<EmptyProjection>(projection.AggregateId));

    // Delete all items created
    var deleted = await RecordStore.DeleteAggregateAsync<EmptyAggregate>(Guid.Empty, aggregate.Id);
    
    var eventsCount = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var snapshotsCount = await RecordStore
      .GetSnapshots<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var projectionsCount = await RecordStore.GetProjections<EmptyProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, eventsCount);
    Assert.Equal(0, snapshotsCount);
    Assert.Equal(0, projectionsCount);
  }

  [Fact]
  public async Task RecordStore_DeleteAggregateAsync_Can_Delete_More_Than_100_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 100; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);
    events.Clear();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);
    var deleted = await RecordStore.DeleteAggregateAsync<EmptyAggregate>(Guid.Empty, aggregate.Id);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(110, deleted);
    Assert.Equal(0, count);
  }
}