using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Can_Get_Aggregate_Version()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Add(new EmptyEvent()));

    await EventStore.AddAsync(events);

    var version = await EventStore.GetAggregateVersionAsync(aggregate.Id);
    
    Assert.Equal(aggregate.Version, version);
  }
  
  [Fact]
  public async Task Get_Version_Of_NonExisting_Aggregate_Throws_EventStoreException()
  {
    await Assert.ThrowsAsync<EventStoreException>(async () =>
      await EventStore.GetAggregateVersionAsync(Guid.NewGuid()));
  }
}