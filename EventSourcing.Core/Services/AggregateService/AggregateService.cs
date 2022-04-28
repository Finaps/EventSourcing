namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
public class AggregateService : IAggregateService
{
  private readonly IRecordStore _store;
  
  /// <summary>
  /// Create Aggregate Service
  /// </summary>
  /// <param name="store"><see cref="IRecordStore"/></param>
  public AggregateService(IRecordStore store) => _store = store;

  /// <inheritdoc />
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAsync<TAggregate>(partitionId, aggregateId, DateTimeOffset.MaxValue, cancellationToken);

  /// <inheritdoc />
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var snapshot = await _store
      .GetSnapshots<TAggregate>()
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable()
      .LastOrDefaultAsync(cancellationToken);

    var index = snapshot?.Index ?? -1;

    var events = _store
      .GetEvents<TAggregate>()
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date && x.Index > index)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable();

    var aggregate = new TAggregate { PartitionId = partitionId, Id = aggregateId };
    await aggregate.RehydrateAsync(snapshot, events, cancellationToken);
    return aggregate.Version == 0 ? null : aggregate;
  }

  /// <inheritdoc />
  public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    await CreateTransaction(aggregate.PartitionId).Add(aggregate).CommitAsync(cancellationToken);
    
    return aggregate;
  }

  /// <inheritdoc />
  public async Task PersistAsync(IEnumerable<Aggregate> aggregates, CancellationToken cancellationToken = default)
  {
    IAggregateTransaction? transaction = null;
    
    foreach (var aggregate in aggregates)
    {
      transaction ??= CreateTransaction(aggregate.PartitionId);
      transaction.Add(aggregate);
    }

    if (transaction != null)
      await transaction.CommitAsync(cancellationToken);
  }

  /// <inheritdoc />
  public IAggregateTransaction CreateTransaction(Guid partitionId) => new AggregateTransaction(_store.CreateTransaction(partitionId));

  /// <inheritdoc />
  public IAggregateTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
}