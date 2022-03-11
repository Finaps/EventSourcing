using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task Can_Add_Event()
  {
    await RecordStore.AddEventsAsync(new Event[] { new EmptyAggregate().Apply(new EmptyEvent()) });
  }

  [Fact]
  public async Task Can_Add_Multiple_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);
  }

  [Fact]
  public async Task Can_Add_Empty_Event_List()
  {
    await RecordStore.AddEventsAsync(Array.Empty<Event>());
  }

  [Fact]
  public async Task Cannot_Add_Null_Event_List()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await RecordStore.AddEventsAsync((List<Event>) null));
  }
  
  [Fact]
  public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version_In_Batch()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent());

    e2 = e2 with { Index = 0 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e1, e2 }));
  }

  [Fact]
  public async Task Cannot_Add_Events_With_Different_AggregateIds_In_Batch()
  {
    var aggregate1 = new EmptyAggregate();
    var event1 = aggregate1.Apply(new EmptyEvent());

    var aggregate2 = new EmptyAggregate();
    var event2 = aggregate2.Apply(new EmptyEvent());

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { event1, event2 }));
  }
  
  [Fact]
  public async Task Cannot_Add_NonConsecutive_Events_In_Batch()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e1, e2 }));
  }
}