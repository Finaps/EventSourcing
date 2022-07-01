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
  public virtual async Task<IAggregateTransaction> AddAggregateAsync(Aggregate aggregate)
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty", nameof(aggregate));

    if (!_aggregates.Add(aggregate))
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate already added.", nameof(aggregate));

    await AddEventsAsync(aggregate.UncommittedEvents);

    foreach (var snapshot in Cache
               .GetSnapshotFactories(aggregate.GetType())
               .Where(x => x.IsSnapshotIntervalExceeded(aggregate))
               .Select(x => x.CreateSnapshot(aggregate)))
      await AddSnapshotAsync(snapshot);

    foreach (var projection in Cache
               .GetProjectionFactories(aggregate.GetType())
               .Select(x => x.CreateProjection(aggregate))
               .OfType<Projection>())
      await UpsertProjectionAsync(projection);

    return this;
  }

  /// <summary>
  /// Add Events to RecordTransaction
  /// </summary>
  /// <param name="events"></param>
  protected virtual Task AddEventsAsync(List<Event> events) =>
    Task.FromResult(_recordTransaction.AddEvents(events));
  
  /// <summary>
  /// Add Snapshot to RecordTransaction
  /// </summary>
  /// <param name="snapshot"></param>
  protected virtual Task AddSnapshotAsync(Snapshot snapshot) =>
    Task.FromResult(_recordTransaction.AddSnapshot(snapshot));
  
  /// <summary>
  /// Add Projection to RecordTransaction
  /// </summary>
  /// <param name="projection"></param>
  protected virtual Task UpsertProjectionAsync(Projection projection) =>
    Task.FromResult(_recordTransaction.UpsertProjection(projection));

  /// <inheritdoc />
  public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await _recordTransaction.CommitAsync(cancellationToken);

    // If Transaction succeeded: Clear all uncommitted Events (they have been committed now)
    foreach (var aggregate in _aggregates) aggregate.UncommittedEvents.Clear();
  }
}