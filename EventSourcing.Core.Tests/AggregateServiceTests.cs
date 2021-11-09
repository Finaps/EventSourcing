using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.MockAggregates;
using EventSourcing.Core.Tests.MockEventStore;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public class AggregateServiceTests
  {
    private readonly IEventStore _eventStore;
    private readonly IAggregateService _aggregateService;

    public AggregateServiceTests()
    {
      _eventStore = new InMemoryEventStore();
      _aggregateService = new AggregateService(_eventStore);
    }

    [Fact]
    public async Task Can_Persist_Event()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);

      var result = await _eventStore.Events.ToListAsync();

      Assert.Single(result);
    }

    [Fact]
    public async Task Cannot_Persist_With_Empty_Id()
    {
      var aggregate = new SimpleAggregate { Id = Guid.Empty };
      aggregate.Add(new EmptyEvent());

      await Assert.ThrowsAsync<ArgumentException>(async () => await _aggregateService.PersistAsync(aggregate));
    }

    [Fact]
    public async Task Can_Persist_Multiple_Events()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      await _aggregateService.PersistAsync(aggregate);

      var result = await _eventStore.Events.ToListAsync();

      Assert.Equal(events.Count, result.Count);
    }

    [Fact]
    public async Task Can_Rehydrate_Aggregate()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      await _aggregateService.PersistAsync(aggregate);

      var rehydrated = await _aggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Equal(events.Count, rehydrated.Counter);
    }
    
    [Fact]
    public async Task Rehydrating_Aggregate_Returns_Null_When_No_Events_Are_Found()
    {
      Assert.Null(await _aggregateService.RehydrateAsync<EmptyAggregate>(Guid.NewGuid()));
    }

    [Fact]
    public async Task Uncommitted_Events_Are_Cleared_After_Persist()
    {
      var aggregate = new SimpleAggregate();

      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);

      Assert.Empty(aggregate.UncommittedEvents);
    }

    [Fact]
    public async Task Empty_Uncommitted_Events_After_Rehydrate()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);

      var result = await _aggregateService.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Empty(result.UncommittedEvents);
    }

    [Fact]
    public async Task Can_Rehydrate_And_Persist()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
      };

      await _aggregateService.PersistAsync(aggregate);
      await _aggregateService.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
        a => a.Add(new EmptyEvent()));

      var result = await _eventStore.Events.ToListAsync();

      Assert.Equal(events.Count + 1, result.Count);
    }
  }
}