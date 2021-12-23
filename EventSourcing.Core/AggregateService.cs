using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Snapshotting;

namespace EventSourcing.Core
{
  public class AggregateService : AggregateService<Event>, IAggregateService
  {
    public AggregateService(IEventStore store) : base(store) { }
  }

  /// <summary>
  /// Aggregate Service: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
  public class AggregateService<TBaseEvent> : IAggregateService<TBaseEvent> where TBaseEvent : Event
  {
    private readonly IEventStore<TBaseEvent> _store;
    
    public AggregateService(IEventStore<TBaseEvent> store)
    {
      _store = store;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (new TAggregate() is ISnapshottable)
        return await RehydrateFromSnapshotAsync<TAggregate>(aggregateId, cancellationToken);

      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();

      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();
      
      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }
    
    private async Task<TAggregate> RehydrateFromSnapshotAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var latestSnapshot = _store.Snapshots
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .LastOrDefault();
      
      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId);

      if (latestSnapshot != null)
        events = events
          .Where(x => x.AggregateVersion >= latestSnapshot.AggregateVersion)
          .Prepend(latestSnapshot);

      var orderedEvents = events.OrderBy(x => x.AggregateVersion).ToAsyncEnumerable();
      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, orderedEvents, cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (aggregate.Id == Guid.Empty)
        throw new ArgumentException("Aggregate.Id cannot be empty", nameof(aggregate));

      await _store.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      aggregate.ClearUncommittedEvents();
      
      if (aggregate is ISnapshottable s && s.IntervalExceeded<TBaseEvent>())
        await CreateAndPersistSnapshotAsync(aggregate, cancellationToken);
      
      return aggregate;
    }

    private async Task CreateAndPersistSnapshotAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (aggregate is not ISnapshottable s)
        throw new InvalidOperationException(
          $"{aggregate.GetType().Name} does not implement {typeof(ISnapshottable)}");
      if (s.CreateSnapshot() is not TBaseEvent snapshot)
        throw new InvalidOperationException(
          $"Snapshot created for {s.GetType().Name} is not of type {nameof(TBaseEvent)}");
      aggregate.Add(snapshot);
      await _store.AddSnapshotAsync(aggregate.UncommittedEvents.Single(), cancellationToken);
      aggregate.ClearUncommittedEvents();
    }
  }
}