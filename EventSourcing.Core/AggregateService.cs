using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Attributes;
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
      // var snapshotInterval = typeof(TAggregate).GetCustomAttributes(typeof(SnapshotInterval)).FirstOrDefault();
      // if (snapshotInterval != null)
      //   return await RehydrateFromSnapshotAsync<TAggregate>(aggregateId, cancellationToken);

      if (new TAggregate() is ISnapshottable<TBaseEvent>)
      {
        return await RehydrateFromSnapshotAsync<TAggregate>(aggregateId, cancellationToken);
      }

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
    
    public async Task<TAggregate> RehydrateFromSnapshotAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var latestSnapshot = _store.Snapshots
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .LastOrDefault();

      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId);

      if (latestSnapshot != null)
      {
        events = events
          .Where(x => x.AggregateVersion > latestSnapshot.AggregateVersion);
        events = _store.Snapshots 
          .Where(x => x.id == latestSnapshot.ToString()) //TODO: Find a better way then fetching snapshot again
          .Union(events);
      }
      
      var orderedEvents = events.OrderBy(x => x.AggregateVersion).ToAsyncEnumerable();
      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, orderedEvents, cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (aggregate.Id == Guid.Empty)
        throw new ArgumentException("Aggregate.Id cannot be empty", nameof(aggregate));

      if (aggregate is ISnapshottable<TBaseEvent> s && s.IntervalExceeded(aggregate.Version))
        aggregate.Add(s.CreateSnapshot());
      
      await _store.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
  }
}