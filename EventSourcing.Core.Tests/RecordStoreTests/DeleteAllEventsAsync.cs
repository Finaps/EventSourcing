namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Can_Delete_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);

    await RecordStore.DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
  
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Can_Only_Delete_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();
    var snapshot = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate)};
    var projection = new EmptyProjection { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Hash = "RANDOM"};

    for (var i = 0; i < 3; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));
    
    
    await RecordStore.AddEventsAsync(events);
    await RecordStore.AddSnapshotAsync(snapshot);
    await RecordStore.UpsertProjectionAsync(projection);
    
    await RecordStore.DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var eventCount = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var snapshotCount = await RecordStore
      .GetSnapshots<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    var projectionResult = await RecordStore.GetProjectionByIdAsync<EmptyProjection>(aggregate.Id);
    
    Assert.Equal(0, eventCount);
    Assert.Equal(1, snapshotCount);
    Assert.NotNull(projectionResult);
  }
  
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Correctly_Returns_Deleted_Count()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);

    var deleted = await RecordStore.DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
    Assert.Equal(events.Count, deleted);
  }
}