namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Can_Delete_Events()
  {
    var aggregate = new EmptyAggregate();
    
    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new EmptyEvent()))
      .ToArray();

    await GetRecordStore().AddEventsAsync(events);

    await GetRecordStore().DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var count = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
  
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Deleting_Events_Does_Not_Delete_Projection()
  {
    var aggregate = new EmptyAggregate();
    var snapshot = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate)};
    var projection = new EmptyProjection { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Hash = "RANDOM"};

    var events = Enumerable
      .Range(0, 3)
      .Select(_ => aggregate.Apply(new EmptyEvent()))
      .ToArray();
    
    await GetRecordStore().AddEventsAsync(events);
    await GetRecordStore().AddSnapshotAsync(snapshot);
    await GetRecordStore().UpsertProjectionAsync(projection);
    
    await GetRecordStore().DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var eventCount = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var projectionResult = await GetRecordStore().GetProjectionByIdAsync<EmptyProjection>(aggregate.Id);
    
    Assert.Equal(0, eventCount);
    Assert.NotNull(projectionResult);
  }
  
  [Fact]
  public async Task RecordStore_DeleteAllEventsAsync_Correctly_Returns_Deleted_Count()
  {
    var aggregate = new EmptyAggregate();
    
    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new EmptyEvent()))
      .ToArray();
    
    await GetRecordStore().AddEventsAsync(events);

    var deleted = await GetRecordStore().DeleteAllEventsAsync<EmptyAggregate>(aggregate.Id);

    var count = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
    Assert.Equal(events.Length, deleted);
  }
}