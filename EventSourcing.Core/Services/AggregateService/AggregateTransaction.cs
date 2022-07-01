namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
public class AggregateTransaction : IAggregateTransaction
{
  protected readonly IRecordTransaction RecordTransaction;
  private readonly HashSet<Aggregate> _aggregates = new();

  /// <summary>
  /// Create Aggregate Transaction
  /// </summary>
  /// <param name="recordTransaction"><see cref="IRecordTransaction"/></param>
  public AggregateTransaction(IRecordTransaction recordTransaction)
  {
    RecordTransaction = recordTransaction;
  }

  /// <inheritdoc />
  public virtual async Task<IAggregateTransaction> AddAggregateAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    if (aggregate.Id == Guid.Empty)
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate.Id cannot be empty", nameof(aggregate));

    if (!_aggregates.Add(aggregate))
      throw new ArgumentException(
        $"Error adding {aggregate} to {nameof(AggregateTransaction)}. Aggregate already added.", nameof(aggregate));

    await AddEventsAsync(aggregate.UncommittedEvents, cancellationToken);

    foreach (var snapshot in Cache
               .GetSnapshotFactories<TAggregate>()
               .Where(x => x.IsSnapshotIntervalExceeded(aggregate))
               .Select(x => x.CreateSnapshot(aggregate)))
      await AddSnapshotAsync(snapshot, cancellationToken);

    foreach (var projection in Cache
               .GetProjectionFactories<TAggregate>()
               .Select(x => x.CreateProjection(aggregate))
               .OfType<Projection>())
      await UpsertProjectionAsync(projection, cancellationToken);

    return this;
  }

  /// <summary>
  /// Add Events to RecordTransaction
  /// </summary>
  /// <param name="events"></param>
  protected virtual Task AddEventsAsync<TAggregate>(List<Event<TAggregate>> events, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new() => Task.FromResult(RecordTransaction.AddEvents(events));
  
  /// <summary>
  /// Add Snapshot to RecordTransaction
  /// </summary>
  /// <param name="snapshot"></param>
  protected virtual Task AddSnapshotAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new() => Task.FromResult(RecordTransaction.AddSnapshot(snapshot));
  
  /// <summary>
  /// Add Projection to RecordTransaction
  /// </summary>
  /// <param name="projection"></param>
  protected virtual Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default) =>
    Task.FromResult(RecordTransaction.UpsertProjection(projection));

  /// <inheritdoc />
  public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await RecordTransaction.CommitAsync(cancellationToken);

    // If Transaction succeeded: Clear all uncommitted Events (they have been committed now)
    foreach (var aggregate in _aggregates) aggregate.ClearUncommittedEvents();
  }
}