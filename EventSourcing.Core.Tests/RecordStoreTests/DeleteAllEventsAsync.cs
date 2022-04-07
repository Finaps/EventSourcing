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
}