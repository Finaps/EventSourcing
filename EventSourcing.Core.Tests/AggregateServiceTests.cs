using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.MockAggregates;
using EventSourcing.Core.Tests.MockDatabase;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public class AggregateServiceTests
  {
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private readonly IAggregateService _aggregateService;

    public AggregateServiceTests()
    {
      _eventStore = new InMemoryEventStore();
      _snapshotStore = new InMemorySnapshotStore();
      _aggregateService = new AggregateService(_eventStore, _snapshotStore, null);
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
    
    [Fact]
    public async Task Rehydrating_Calls_Finish()
    {
      var aggregate = new VerboseAggregate();
      aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);
      
      Assert.False(aggregate.IsFinished);

      var result = await _aggregateService.RehydrateAsync<VerboseAggregate>(aggregate.Id);

      Assert.True(result.IsFinished);
    }
    
    [Fact]
    public async Task Can_Snapshot_Aggregate()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength;
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);
      
      Assert.Single(_snapshotStore.Snapshots);
      Assert.Equal((int) aggregate.IntervalLength, _eventStore.Events.Count());
    }
    
    [Fact]
    public async Task Cannot_Snapshot_When_Interval_Is_Not_Exceeded()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength - 1;
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);
      
      Assert.Empty(_snapshotStore.Snapshots);
      Assert.Equal((int) eventsCount, _eventStore.Events.Count());
    }
    
    [Fact]
    public async Task One_Snapshot_When_Interval_Is_Exceeded_Twice()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = 2 * aggregate.IntervalLength;
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);
      
      Assert.Single(_snapshotStore.Snapshots);
      Assert.Equal((int) eventsCount, _eventStore.Events.Count());
    }
    
    [Fact]
    public async Task Can_Rehydrate_Snapshotted_Aggregate()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength;
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());

      await _aggregateService.PersistAsync(aggregate);
      var result = await _aggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
      
      Assert.NotNull(result);
      Assert.Equal((int) eventsCount, result.Counter);
      Assert.Equal(0, result.EventsAppliedAfterHydration);
      Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
    }
    
    [Fact]
    public async Task Can_Rehydrate_Twice_Snapshotted_Aggregate()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength;
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      var result = await _aggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
      
      Assert.NotNull(result);
      Assert.Equal(2 * (int) eventsCount, result.Counter);
      Assert.Equal(0, result.EventsAppliedAfterHydration);
      Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
      Assert.Equal(2, _snapshotStore.Snapshots.Count());
    }
    
    [Fact]
    public async Task Can_Rehydrate_From_Snapshot_And_Events()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength;
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount - 1])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      var result = await _aggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
      
      Assert.NotNull(result);
      Assert.Equal(2 * (int) eventsCount - 1, result.Counter);
      Assert.Equal((int) eventsCount - 1, result.EventsAppliedAfterHydration);
      Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
      Assert.Single(_snapshotStore.Snapshots);
    }
    
    [Fact]
    public async Task Can_Rehydrate_Multiple_Snapshotted_Aggregate()
    {
      var aggregate = new SnapshotAggregate();
      var eventsCount = aggregate.IntervalLength;
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      foreach (var _ in new int[eventsCount - 1])
        aggregate.Add(new EmptyEvent());
      await _aggregateService.PersistAsync(aggregate);
      
      var result = await _aggregateService.RehydrateAsync<SnapshotAggregate>(aggregate.Id);
      
      Assert.NotNull(result);
      Assert.Equal(5 * (int) eventsCount - 1, result.Counter);
      Assert.Equal((int) eventsCount - 1, result.EventsAppliedAfterHydration);
      Assert.Equal(1, result.SnapshotsAppliedAfterHydration);
      Assert.Equal(4, _snapshotStore.Snapshots.Count());
    }
  }
}