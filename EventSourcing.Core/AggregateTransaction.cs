using Microsoft.Extensions.Logging;

namespace EventSourcing.Core;

public class AggregateTransaction<TBaseEvent> : IAggregateTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  private readonly IEventTransaction<TBaseEvent> _eventTransaction;
  private readonly ISnapshotStore<TBaseEvent> _snapshotStore;
  private readonly ILogger<AggregateService<TBaseEvent>> _logger;
  private readonly List<Aggregate<TBaseEvent>> _aggregates = new();

  public AggregateTransaction(
    IEventTransaction<TBaseEvent> eventTransaction,
    ISnapshotStore<TBaseEvent> snapshotStore,
    ILogger<AggregateService<TBaseEvent>> logger)
  {
    _eventTransaction = eventTransaction;
    _snapshotStore = snapshotStore;
    _logger = logger;
  }
  
  public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TBaseEvent>, new()
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException("Aggregate.Id cannot be empty", nameof(aggregate));

    await _eventTransaction.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
    
    _aggregates.Add(aggregate);

    return aggregate;
  }

  public async Task CommitAsync(CancellationToken cancellationToken)
  {
    await _eventTransaction.CommitAsync(cancellationToken);
    
    foreach (var aggregate in _aggregates)
    {
      if (aggregate is ISnapshottable s && s.IntervalExceeded<TBaseEvent>())
        if (_snapshotStore != null)
        {
          aggregate.ClearUncommittedEvents();
          await CreateAndPersistSnapshotAsync(aggregate, cancellationToken);
        }
        else
        {
          _logger?.LogWarning(
            "{SnapshotStore} not provided while {TAggregate} implements {ISnapshottable}. No snapshot created",
            typeof(ISnapshotStore<TBaseEvent>), aggregate.GetType(), typeof(ISnapshottable));
        }
      else
      {
        aggregate.ClearUncommittedEvents();
      }
    }
  }
  
  private async Task CreateAndPersistSnapshotAsync(Aggregate<TBaseEvent> aggregate,
    CancellationToken cancellationToken = default)
  {
    if (_snapshotStore == null)
      throw new InvalidOperationException("Snapshot store not provided");
    if (aggregate is not ISnapshottable s)
      throw new InvalidOperationException(
        $"{aggregate.GetType().Name} does not implement {typeof(ISnapshottable)}");
    if (s.CreateSnapshot() is not TBaseEvent snapshot)
      throw new InvalidOperationException(
        $"Snapshot created for {s.GetType().Name} is not of type {nameof(TBaseEvent)}");
    
    await _snapshotStore.AddSnapshotAsync(aggregate.Add(snapshot), cancellationToken);
    aggregate.ClearUncommittedEvents();
  }
}