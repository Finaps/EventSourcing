namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Single_Event()
  {
    await RecordStore.AddEventsAsync(new Event[] { new EmptyAggregate().Apply(new EmptyEvent()) });
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Multiple_Events()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>();

    for (var i = 0; i < 10; i++)
      events.Add(aggregate.Apply(new EmptyEvent()));

    await RecordStore.AddEventsAsync(events);
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Can_Add_Empty_List()
  {
    await RecordStore.AddEventsAsync(Array.Empty<Event>());
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await RecordStore.AddEventsAsync(null));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Duplicate_Events()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent());

    e2 = e2 with { Index = 0 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e1, e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Duplicate_Events_In_Separate_Calls()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());

    await RecordStore.AddEventsAsync(new Event[] { e1 });
    
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 0 };

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e2 }));
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Events_With_Different_AggregateIds()
  {
    var aggregate1 = new EmptyAggregate();
    var event1 = aggregate1.Apply(new EmptyEvent());

    var aggregate2 = new EmptyAggregate();
    var event2 = aggregate2.Apply(new EmptyEvent());

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { event1, event2 }));
  }

  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_NonConsecutive_Events()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e1, e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_NonConsecutive_Events_In_Separate_Calls()
  {
    var aggregate = new EmptyAggregate();
    var e1 = aggregate.Apply(new EmptyEvent());

    await RecordStore.AddEventsAsync(new Event[] { e1 });
    
    var e2 = aggregate.Apply(new EmptyEvent()) with { Index = 2 };

    await Assert.ThrowsAnyAsync<RecordStoreException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e2 }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Negative_Index()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { Index = -1 };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Null_Type()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { Type = null };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e }));
  }
  
  [Fact]
  public async Task RecordStore_AddEventsAsync_Cannot_Add_Event_With_Null_AggregateType()
  {
    var aggregate = new EmptyAggregate();
    var e = aggregate.Apply(new EmptyEvent()) with { AggregateType = null };
    
    await Assert.ThrowsAnyAsync<RecordValidationException>(
      async () => await RecordStore.AddEventsAsync(new Event[] { e }));
  }
}