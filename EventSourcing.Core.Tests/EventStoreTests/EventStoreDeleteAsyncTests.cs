using EventSourcing.Core.Records;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Can_Delete_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Add(new EmptyEvent()));

    await EventStore.AddAsync(events);

    await EventStore.DeleteAsync(aggregate.RecordId);

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.RecordId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
}