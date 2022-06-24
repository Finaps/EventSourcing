namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
public class AggregateTransaction : IAggregateTransaction
{
  private readonly IRecordTransaction _recordTransaction;
  private readonly HashSet<Aggregate> _aggregates = new();

  /// <summary>
  /// Create Aggregate Transaction
  /// </summary>
  /// <param name="recordTransaction"><see cref="IRecordTransaction"/></param>
  public AggregateTransaction(IRecordTransaction recordTransaction)
  {
    _recordTransaction = recordTransaction;
  }

  /// <inheritdoc />
  public IAggregateTransaction Add(Aggregate aggregate)
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty", nameof(aggregate));

    if (!_aggregates.Add(aggregate))
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate already added.", nameof(aggregate));

    _recordTransaction.AddEvents(aggregate.UncommittedEvents);

    foreach (var snapshot in EventSourcingCache
               .GetSnapshotFactories(aggregate.GetType())
               .Where(factory => factory.IsSnapshotIntervalExceeded(aggregate))
               .Select(factory => factory.CreateSnapshot(aggregate)))
      _recordTransaction.AddSnapshot(snapshot);

    foreach (var projection in EventSourcingCache
               .GetProjectionFactories(aggregate.GetType())
               .Select(x => x.CreateProjection(aggregate)))
      _recordTransaction.UpsertProjection(projection);

    return this;
  }

  /// <inheritdoc />
  public async Task CommitAsync(CancellationToken cancellationToken)
  {
    await _recordTransaction.CommitAsync(cancellationToken);

    // If Transaction succeeded: Clear all uncommitted Events (they have been committed now)
    foreach (var aggregate in _aggregates) aggregate.UncommittedEvents.Clear();
  }
}