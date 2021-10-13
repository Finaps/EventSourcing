using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
    public abstract class EventStoreTests
  {
    public abstract IEventStore Store { get; }
    
    [Fact]
    public async Task Can_Add_Event()
    {
      var aggregate = new EmptyAggregate();
      var @event = Event.Create<EmptyEvent>(aggregate);
      await Store.AddAsync(new Event[] { @event });
    }

    [Fact]
    public async Task Can_Add_Multiple_Events()
    {
      var aggregate = new EmptyAggregate();
      var events = new List<Event>();

      for (var i = 0; i < 10; i++)
        events.Add(aggregate.Add(Event.Create<EmptyEvent>(aggregate)));
      
      await Store.AddAsync(events);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_Id()
    {
      var aggregate = new EmptyAggregate();
      var @event = Event.Create<EmptyEvent>(aggregate);

      await Store.AddAsync(new Event[] { @event });

      var exception = await Assert.ThrowsAnyAsync<EventStoreException>(
        async () => await Store.AddAsync(new Event[] { @event }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_Id_In_Batch()
    {
      var aggregate = new EmptyAggregate();
      var @event = Event.Create<EmptyEvent>(aggregate);

      var exception = await Assert.ThrowsAnyAsync<EventStoreException>(
        async () => await Store.AddAsync(new Event[] { @event, @event }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version()
    {
      var aggregate = new EmptyAggregate();
      var event1 = Event.Create<EmptyEvent>(aggregate);
      var event2 = Event.Create<EmptyEvent>(aggregate);

      await Store.AddAsync(new Event[] { event1 });

      var exception = await Assert.ThrowsAnyAsync<EventStoreException>(
        async () => await Store.AddAsync(new Event[] { event2 }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version_In_Batch()
    {
      var aggregate = new EmptyAggregate();
      var event1 = Event.Create<EmptyEvent>(aggregate);
      var event2 = Event.Create<EmptyEvent>(aggregate);

      var exception = await Assert.ThrowsAnyAsync<EventStoreException>(
        async () => await Store.AddAsync(new Event[] { event1, event2 }));

      Assert.IsType<ConflictException>(exception.InnerException);
    }

    [Fact]
    public async Task Cannot_Add_Events_With_Different_AggregateIds_In_Batch()
    {
      var aggregate1 = new EmptyAggregate();
      var event1 = Event.Create<EmptyEvent>(aggregate1);
      var aggregate2 = new EmptyAggregate();
      var event2 = Event.Create<EmptyEvent>(aggregate2);

      await Assert.ThrowsAnyAsync<EventStoreException>(
        async () => await Store.AddAsync(new Event[] { event1, event2 }));
    }

    [Fact]
    public async Task Can_Get_Events_By_AggregateId()
    {
      var aggregate = new EmptyAggregate();
      var events = new List<Event>
      {
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate))
      };

      await Store.AddAsync(events);

      var result = await Store.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .ToListAsync();
       
      
      Assert.Equal(events.Count, result.Count);
    }
    
    [Fact]
    public async Task Can_Filter_Events()
    {
      var aggregate = new EmptyAggregate();
      var events = new List<Event>
      {
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate))
      };
      
      var aggregate2 = new EmptyAggregate();
      var events2 = new List<Event>
      {
        aggregate2.Add(Event.Create<EmptyEvent>(aggregate2)),
        aggregate2.Add(Event.Create<EmptyEvent>(aggregate2))
      };

      await Store.AddAsync(events);
      await Store.AddAsync(events2);

      var result = await Store.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .Where(x => x.AggregateVersion > 0)
        .ToListAsync();
       
      
      Assert.Single(result);
    }
  }
}
