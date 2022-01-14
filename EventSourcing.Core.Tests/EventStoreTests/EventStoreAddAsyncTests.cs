using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Can_Add_Event()
  {
    await EventStore.AddAsync(new Event[] { new EmptyAggregate().Add(new EmptyEvent()) });
  }

  [Fact]
  public async Task Can_Add_Multiple_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Add(new EmptyEvent()));

    await EventStore.AddAsync(events);
  }

  [Fact]
  public async Task Can_Add_Empty_Event_List()
  {
    await EventStore.AddAsync(Array.Empty<Event>());
  }

  [Fact]
  public async Task Cannot_Add_Null_Event_List()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(async () => await EventStore.AddAsync(null));
  }
  
  [Fact]
  public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version_In_Batch()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Add(new EmptyEvent());
    var e2 = aggregate.Add(new EmptyEvent());

    e2 = e2 with { AggregateVersion = 0 };

    await Assert.ThrowsAnyAsync<ArgumentException>(
      async () => await EventStore.AddAsync(new Event[] { e1, e2 }));
  }

  [Fact]
  public async Task Cannot_Add_Events_With_Different_AggregateIds_In_Batch()
  {
    var aggregate1 = new EmptyAggregate();
    var event1 = aggregate1.Add(new EmptyEvent());

    var aggregate2 = new EmptyAggregate();
    var event2 = aggregate2.Add(new EmptyEvent());

    await Assert.ThrowsAnyAsync<ArgumentException>(
      async () => await EventStore.AddAsync(new Event[] { event1, event2 }));
  }
  
  [Fact]
  public async Task Cannot_Add_NonConsecutive_Events_In_Batch()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Add(new EmptyEvent());
    var e2 = aggregate.Add(new EmptyEvent()) with { AggregateVersion = 2 };

    await Assert.ThrowsAnyAsync<ArgumentException>(
      async () => await EventStore.AddAsync(new Event[] { e1, e2 }));
  }
}