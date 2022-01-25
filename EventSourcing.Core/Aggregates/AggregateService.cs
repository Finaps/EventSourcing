using Microsoft.Extensions.Logging;

namespace EventSourcing.Core;

public class AggregateService : IAggregateService
{
  private readonly IEventStore _eventStore;
  private readonly ISnapshotStore _snapshotStore;
  private readonly ILogger<AggregateService> _logger;
  
  public AggregateService(IEventStore eventStore, ISnapshotStore snapshotStore = null, ILogger<AggregateService> logger = null)
  {
    _eventStore = eventStore;
    _snapshotStore = snapshotStore;
    _logger = logger;
  }

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAsync<TAggregate>(partitionId, aggregateId, DateTimeOffset.MaxValue, cancellationToken);

  public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var interval = new TAggregate().SnapshotInterval;
    
    if (interval > 0 && _snapshotStore == null)
      _logger?.LogWarning("{SnapshotStore} not provided while {TAggregate} has snapshot interval {interval}. " +
                          "Rehydrating from events only", typeof(ISnapshotStore), typeof(TAggregate), interval);

    Snapshot snapshot = null;
    
    if (interval > 0 && _snapshotStore != null)
      snapshot = await _snapshotStore.Snapshots
      .Where(x => 
        x.PartitionId == partitionId &&
        x.AggregateId == aggregateId &&
        x.Timestamp <= date)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable()
      .LastOrDefaultAsync(cancellationToken);

    var index = snapshot?.Index ?? -1;

    var events = _eventStore.Events
      .Where(x =>
        x.PartitionId == partitionId &&
        x.AggregateId == aggregateId &&
        x.Timestamp <= date &&
        x.Index > index)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable();

    return await Aggregate.RehydrateAsync<TAggregate>(partitionId, aggregateId, snapshot, events, cancellationToken);
  }

  public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    await CreateTransaction(aggregate.PartitionId)
      .Add(aggregate)
      .CommitAsync(cancellationToken);
    return aggregate;
  }

  public async Task PersistAsync(IEnumerable<Aggregate> aggregates, CancellationToken cancellationToken = default)
  {
    IAggregateTransaction transaction = null;
    
    foreach (var aggregate in aggregates)
    {
      transaction ??= CreateTransaction(aggregate.PartitionId);
      transaction.Add(aggregate);
    }

    if (transaction != null)
      await transaction.CommitAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    await CreateTransaction(partitionId)
      .Delete(aggregateId, await _eventStore.GetVersionAsync(partitionId, aggregateId, cancellationToken))
      .CommitAsync(cancellationToken);
  }

  public IAggregateTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IAggregateTransaction CreateTransaction(Guid partitionId) =>
    new AggregateTransaction(_eventStore.CreateTransaction(partitionId), _snapshotStore, _logger);
}