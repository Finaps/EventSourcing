using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.MockAggregates;
using EventSourcing.Core.Tests.MockEventStore;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public class EventServiceTests
  {
    private readonly IEventStore _eventStore;
    private readonly IEventService _eventService;

    public EventServiceTests()
    {
      _eventStore = new InMemoryEventStore();
      _eventService = new EventService(_eventStore);
    }

    [Fact]
    public async Task Can_Persist_Event()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(Event.Create<EmptyEvent>(aggregate));

      await _eventService.PersistAsync(aggregate);

      var result = await _eventStore.Events.ToListAsync();

      Assert.Single(result);
    }

    [Fact]
    public async Task Cannot_Persist_With_Empty_Id()
    {
      var aggregate = new SimpleAggregate { Id = Guid.Empty };
      aggregate.Add(Event.Create<EmptyEvent>(aggregate));

      await Assert.ThrowsAsync<ArgumentException>(async () => await _eventService.PersistAsync(aggregate));
    }

    [Fact]
    public async Task Can_Persist_Multiple_Events()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate))
      };

      await _eventService.PersistAsync(aggregate);

      var result = await _eventStore.Events.ToListAsync();

      Assert.Equal(events.Count, result.Count);
    }

    [Fact]
    public async Task Can_Rehydrate_Aggregate()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate))
      };

      await _eventService.PersistAsync(aggregate);

      var rehydrated = await _eventService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Equal(events.Count, rehydrated.Counter);
    }

    [Fact]
    public async Task Uncommitted_Events_Are_Cleared_After_Persist()
    {
      var aggregate = new SimpleAggregate();

      aggregate.Add(Event.Create<EmptyEvent>(aggregate));
      aggregate.Add(Event.Create<EmptyEvent>(aggregate));

      await _eventService.PersistAsync(aggregate);

      Assert.Empty(aggregate.UncommittedEvents);
    }

    [Fact]
    public async Task Empty_Uncommitted_Events_After_Rehydrate()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(Event.Create<EmptyEvent>(aggregate));
      aggregate.Add(Event.Create<EmptyEvent>(aggregate));

      await _eventService.PersistAsync(aggregate);

      var result = await _eventService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Empty(result.UncommittedEvents);
    }

    [Fact]
    public async Task Can_Rehydrate_And_Persist()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
        aggregate.Add(Event.Create<EmptyEvent>(aggregate)),
      };

      await _eventService.PersistAsync(aggregate);
      await _eventService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
        a => a.Add(Event.Create<EmptyEvent>(a)));

      var result = await _eventStore.Events.ToListAsync();

      Assert.Equal(events.Count + 1, result.Count);
    }
  }
}