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
public class AggregateService<TBaseEvent> : IAggregateService<TBaseEvent> where TBaseEvent : Event, new()
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

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (new TAggregate() is ISnapshottable)
    {
      if (_snapshotStore != null)
        return await RehydrateFromSnapshotAsync<TAggregate>(partitionId, aggregateId, cancellationToken);

      _logger?.LogWarning("{SnapshotStore} not provided while {TAggregate} implements {ISnapshottable}. Rehydrating from events only", 
        typeof(ISnapshotStore<TBaseEvent>),typeof(TAggregate),typeof(ISnapshottable));
    }

    var events = _eventStore.Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .OrderBy(x => x.AggregateVersion)
      .AsAsyncEnumerable();

    return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(partitionId, aggregateId, events, cancellationToken);
  }

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    var events = _eventStore.Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date)
      .OrderBy(x => x.AggregateVersion)
      .AsAsyncEnumerable();
      
    return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(partitionId, aggregateId, events, cancellationToken);
  }
    
  private async Task<TAggregate> RehydrateFromSnapshotAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (_snapshotStore == null)
      throw new InvalidOperationException("Snapshot store not provided");
      
    var latestSnapshot = await _snapshotStore.Snapshots
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .OrderBy(x => x.AggregateVersion)
      .AsAsyncEnumerable()
      .LastOrDefaultAsync(cancellationToken);

    var version = latestSnapshot?.AggregateVersion ?? 0;

    var events = _eventStore.Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.AggregateVersion >= version)
      .OrderBy(x => x.AggregateVersion)
      .AsAsyncEnumerable();

    if (latestSnapshot != null)
      events = events.Prepend(latestSnapshot);

    return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(partitionId, aggregateId, events, cancellationToken);
  }

  public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
  {
    var transaction = CreateTransaction(aggregate.PartitionId);
    await transaction.AddAsync(aggregate, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return aggregate;
  }

  public async Task PersistAsync(IEnumerable<Aggregate<TBaseEvent>> aggregates, CancellationToken cancellationToken = default)
  {
    IAggregateTransaction<TBaseEvent> transaction = null;
    
    foreach (var aggregate in aggregates)
    {
      transaction ??= CreateTransaction(aggregate.PartitionId);
      await transaction.AddAsync(aggregate, cancellationToken);
    }

    if (transaction != null)
      await transaction.CommitAsync(cancellationToken);
  }

  public IAggregateTransaction<TBaseEvent> CreateTransaction() => CreateTransaction(Guid.Empty);
  public IAggregateTransaction<TBaseEvent> CreateTransaction(Guid partitionId) =>
    new AggregateTransaction<TBaseEvent>(_eventStore.CreateTransaction(partitionId), _snapshotStore, _logger);
}