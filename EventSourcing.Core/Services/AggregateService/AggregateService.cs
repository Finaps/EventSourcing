using System.Linq.Expressions;

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
  public virtual async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var snapshot = await GetLatestSnapshotAsync<TAggregate>(partitionId, aggregateId, date, cancellationToken);
    var events = await GetEventStreamAsync<TAggregate>(partitionId, aggregateId, date, snapshot?.Index ?? -1);

    var aggregate = new TAggregate { PartitionId = partitionId, Id = aggregateId };
    await aggregate.RehydrateAsync(snapshot, events, cancellationToken);
    return aggregate.Version == 0 ? null : aggregate;
  }

  /// <inheritdoc />
  public async Task PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) 
    where TAggregate : Aggregate, new() => await PersistAsync(new[] { aggregate }, cancellationToken);

  /// <inheritdoc />
  public virtual async Task PersistAsync(IEnumerable<Aggregate> aggregates, CancellationToken cancellationToken = default)
  {
    IAggregateTransaction? transaction = null;
    
    foreach (var aggregate in aggregates)
    {
      transaction ??= CreateTransaction(aggregate.PartitionId);
      await transaction.AddAggregateAsync(aggregate);
    }

    if (transaction != null)
      await transaction.CommitAsync(cancellationToken);
  }

  /// <inheritdoc />
  public IAggregateTransaction CreateTransaction(Guid partitionId) => new AggregateTransaction(_store.CreateTransaction(partitionId));

  /// <inheritdoc />
  public IAggregateTransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  /// <summary>
  /// Get latest <see cref="Snapshot"/> before <paramref name="date"/>
  /// </summary>
  /// <param name="partitionId"><see cref="Snapshot"/>.<see cref="Record.PartitionId"/> to query</param>
  /// <param name="aggregateId"><see cref="Snapshot"/>.<see cref="Record.AggregateId"/> to query</param>
  /// <param name="date">latest <see cref="DateTimeOffset"/> to query</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <returns><see cref="Snapshot"/> or <c>null</c></returns>
  protected virtual async Task<Snapshot?> GetLatestSnapshotAsync<TAggregate>(
    Guid partitionId, Guid aggregateId, DateTimeOffset date, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new()
  {
    // Save performance by only filtering the necessary columns
    Expression<Func<Snapshot, bool>> filter = date == DateTimeOffset.MaxValue
      ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId
      : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date;

    return await _store
      .GetSnapshots<TAggregate>()
      .Where(filter)
      .OrderByDescending(x => x.Index)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Get <see cref="Event"/> stream before or on <paramref name="date"/>
  /// </summary>
  /// <param name="partitionId"><see cref="Event"/>.<see cref="Record.PartitionId"/> to query</param>
  /// <param name="aggregateId"><see cref="Event"/>.<see cref="Record.AggregateId"/> to query</param>
  /// <param name="date">latest <see cref="DateTimeOffset"/> to query</param>
  /// <param name="after"><see cref="Event"/>.<see cref="Event.Index"/> should be greater then (not equal to) <paramref name="after"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <returns><see cref="IAsyncEnumerable{T}"/> of <see cref="Event"/></returns>
  protected virtual Task<IAsyncEnumerable<Event>> GetEventStreamAsync<TAggregate>(
    Guid partitionId, Guid aggregateId, DateTimeOffset date, long after)
    where TAggregate : Aggregate, new()
  {
    // Save performance by only filtering the necessary columns
    Expression<Func<Event, bool>> filter = date == DateTimeOffset.MaxValue
      ? after == -1
        ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId
        : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Index > after
      : after == -1
        ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date
        : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date && x.Index > after;

      return Task.FromResult(_store
        .GetEvents<TAggregate>()
        .Where(filter)
        .OrderBy(x => x.Index)
        .AsAsyncEnumerable());
    }
}