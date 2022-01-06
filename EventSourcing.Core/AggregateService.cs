using Microsoft.Extensions.Logging;

namespace EventSourcing.Core;

public class AggregateService : AggregateService<Event>, IAggregateService
{
  public AggregateService(IEventStore eventStore, ISnapshotStore snapshotStore, ILogger<AggregateService> logger) 
    : base(eventStore, snapshotStore, logger) { }
}

/// <summary>
/// Aggregate Service: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
/// </summary>
/// <typeparam name="TBaseEvent"></typeparam>
public class AggregateService<TBaseEvent> : IAggregateService<TBaseEvent> where TBaseEvent : Event
{
  private readonly IEventStore<TBaseEvent> _eventStore;
  private readonly ISnapshotStore<TBaseEvent> _snapshotStore;
  private readonly ILogger<AggregateService<TBaseEvent>> _logger;
    
  public AggregateService(
    IEventStore<TBaseEvent> eventStore,
    ISnapshotStore<TBaseEvent> snapshotStore,
    ILogger<AggregateService<TBaseEvent>> logger)
  {
    _eventStore = eventStore;
    _snapshotStore = snapshotStore;
    _logger = logger;
  }

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (new TAggregate() is ISnapshottable)
    {
      if (_snapshotStore != null)
        return await RehydrateFromSnapshotAsync<TAggregate>(aggregateId, cancellationToken);

      _logger?.LogWarning("{SnapshotStore} not provided while {TAggregate} implements {ISnapshottable}. Rehydrating from events only", 
        typeof(ISnapshotStore<TBaseEvent>),typeof(TAggregate),typeof(ISnapshottable));
    }

    var events = _eventStore.Events
      .Where(x => x.AggregateId == aggregateId)
      .OrderBy(x => x.AggregateVersion)
      .ToAsyncEnumerable();

    return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
  }

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    var events = _eventStore.Events
      .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
      .OrderBy(x => x.AggregateVersion)
      .ToAsyncEnumerable();
      
    return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
  }
    
  private async Task<TAggregate> RehydrateFromSnapshotAsync<TAggregate>(Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (_snapshotStore == null)
      throw new InvalidOperationException("Snapshot store not provided");
      
    var latestSnapshot = _snapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregateId)
      .OrderBy(x => x.AggregateVersion)
      .LastOrDefault();
      
    var events = _eventStore.Events
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

    await _eventStore.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);

    if (aggregate is ISnapshottable s && s.IntervalExceeded<TBaseEvent>())
    {
      aggregate.ClearUncommittedEvents();
        
      if (_snapshotStore != null) 
        return await CreateAndPersistSnapshotAsync(aggregate, cancellationToken);
        
      _logger?.LogWarning(
        "{SnapshotStore} not provided while {TAggregate} implements {ISnapshottable}. No snapshot created",
        typeof(ISnapshotStore<TBaseEvent>), typeof(TAggregate), typeof(ISnapshottable));
        
      return aggregate;
    }
      
    aggregate.ClearUncommittedEvents();
    return aggregate;
  }

  private async Task<TAggregate> CreateAndPersistSnapshotAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (_snapshotStore == null)
      throw new InvalidOperationException("Snapshot store not provided");
    if (aggregate is not ISnapshottable s)
      throw new InvalidOperationException(
        $"{aggregate.GetType().Name} does not implement {typeof(ISnapshottable)}");
    if (s.CreateSnapshot() is not TBaseEvent snapshot)
      throw new InvalidOperationException(
        $"Snapshot created for {s.GetType().Name} is not of type {nameof(TBaseEvent)}");
      
    aggregate.Add(snapshot);
    await _snapshotStore.AddSnapshotAsync(aggregate.UncommittedEvents.Single(), cancellationToken);
    aggregate.ClearUncommittedEvents();
    return aggregate;
  }
}