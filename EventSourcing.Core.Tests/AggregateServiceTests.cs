using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public abstract class AggregateServiceTests
  {
    protected abstract IEventStore GetEventStore();
    protected abstract IViewStore GetViewStore();
    protected abstract IAggregateService GetAggregateService();

    [Fact]
    public async Task Can_Persist_Event()
    {
      var service = GetAggregateService();
      var store = GetEventStore();
      
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());

      await service.PersistAsync(aggregate);

      var result = await store.Events.ToListAsync();

      Assert.Single(result);
    }

    [Fact]
    public async Task Cannot_Persist_With_Empty_Id()
    {
      var service = GetAggregateService();

      var aggregate = new SimpleAggregate { Id = Guid.Empty };
      aggregate.Add(new EmptyEvent());

      await Assert.ThrowsAsync<ArgumentException>(async () => await service.PersistAsync(aggregate));
    }

    [Fact]
    public async Task Can_Persist_Multiple_Events()
    {
      var service = GetAggregateService();
      var store = GetEventStore();
      
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      await service.PersistAsync(aggregate);

      var result = await store.Events.ToListAsync();

      Assert.Equal(events.Count, result.Count);
    }

    [Fact]
    public async Task Can_Rehydrate_Aggregate()
    {
      var service = GetAggregateService();

      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      await service.PersistAsync(aggregate);

      var rehydrated = await service.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Equal(events.Count, rehydrated.Counter);
    }
    
    [Fact]
    public async Task Rehydrating_Aggregate_Returns_Null_When_No_Events_Are_Found()
    {
      Assert.Null(await GetAggregateService().RehydrateAsync<EmptyAggregate>(Guid.NewGuid()));
    }

    [Fact]
    public async Task Uncommitted_Events_Are_Cleared_After_Persist()
    {
      var service = GetAggregateService();

      var aggregate = new SimpleAggregate();

      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());

      await service.PersistAsync(aggregate);

      Assert.Empty(aggregate.UncommittedEvents);
    }

    [Fact]
    public async Task Empty_Uncommitted_Events_After_Rehydrate()
    {
      var service = GetAggregateService();

      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());

      await service.PersistAsync(aggregate);

      var result = await service.RehydrateAsync<SimpleAggregate>(aggregate.Id);

      Assert.Empty(result.UncommittedEvents);
    }

    [Fact]
    public async Task Can_Rehydrate_And_Persist()
    {
      var service = GetAggregateService();
      var store = GetEventStore();
      
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
      };

      await service.PersistAsync(aggregate);
      await service.RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
        a => a.Add(new EmptyEvent()));

      var result = await store.Events.ToListAsync();

      Assert.Equal(events.Count + 1, result.Count);
    }
    
    [Fact]
    public async Task Rehydrating_Calls_Finish()
    {
      var service = GetAggregateService();
      
      var aggregate = new VerboseAggregate();
      aggregate.Add(new EmptyEvent());

      await service.PersistAsync(aggregate);
      
      Assert.False(aggregate.IsFinished);

      var result = await service.RehydrateAsync<VerboseAggregate>(aggregate.Id);

      Assert.True(result.IsFinished);
    }
  }
}
