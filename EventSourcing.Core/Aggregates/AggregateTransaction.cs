using Microsoft.Extensions.Logging;

namespace EventSourcing.Core;

public class AggregateTransaction : IAggregateTransaction
{
  private readonly IEventTransaction _eventTransaction;
  private readonly ISnapshotStore? _snapshotStore;
  private readonly ILogger<AggregateService>? _logger;
  private readonly HashSet<Aggregate> _aggregates = new();

  public AggregateTransaction(IEventTransaction eventTransaction, ISnapshotStore? snapshotStore, ILogger<AggregateService>? logger)
  {
    _eventTransaction = eventTransaction;
    _snapshotStore = snapshotStore;
    _logger = logger;
  }
  
  public IAggregateTransaction Add(Aggregate aggregate)
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException($"Error adding {aggregate.Format()} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty", nameof(aggregate));
    
    if (!_aggregates.Add(aggregate))
      throw new ArgumentException($"Error adding {aggregate.Format()} to {nameof(AggregateTransaction)}. Aggregate already added.", nameof(aggregate));

    _eventTransaction.Add(aggregate.UncommittedEvents.ToList());

    return this;
  }

  public IAggregateTransaction Delete(Guid aggregateId, long aggregateVersion)
  {
    _eventTransaction.Delete(aggregateId, aggregateVersion);

    return this;
  }
    

  public async Task CommitAsync(CancellationToken cancellationToken)
  {
    await _eventTransaction.CommitAsync(cancellationToken);

    try
    {
      foreach (var aggregate in _aggregates)
      {
        if (
          // If the Snapshot Store was provided
          _snapshotStore != null &&

          // If Aggregate is includes snapshots
          aggregate.SnapshotInterval > 0 &&

          // If the Snapshot Interval Threshold has been met
          aggregate.IsSnapshotIntervalExceeded())
        {
          // Create Snapshot
          await _snapshotStore.AddAsync(aggregate.CreateLinkedSnapshot(), cancellationToken);
        }
        
        // Warn when Aggregate can create snapshots, but no Snapshot Store has been provided
        if (aggregate.SnapshotInterval > 0 && _snapshotStore == null)
          _logger?.LogWarning(
            "{SnapshotStore} not provided while {TAggregate} has snapshot interval {interval}. Cannot create snapshot.",
            typeof(ISnapshotStore), aggregate.GetType(), aggregate.SnapshotInterval);
      }
    }
    finally
    {
      foreach (var aggregate in _aggregates)
        aggregate.ClearUncommittedEvents();
    }
  }
}