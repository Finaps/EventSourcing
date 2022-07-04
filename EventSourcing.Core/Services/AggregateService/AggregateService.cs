using System.Linq.Expressions;

namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
public class AggregateService : IAggregateService
{
  /// <summary>
  /// <see cref="IRecordStore"/>
  /// </summary>
  protected readonly IRecordStore Store;
  
  /// <summary>
  /// Create Aggregate Service
  /// </summary>
  /// <param name="store"><see cref="IRecordStore"/></param>
  public AggregateService(IRecordStore store) => Store = store;

  /// <inheritdoc />
  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await RehydrateAsync<TAggregate>(partitionId, aggregateId, DateTimeOffset.MaxValue, cancellationToken);

  /// <inheritdoc />
  public virtual async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new()
  {
    var snapshot = await GetLatestSnapshotAsync<TAggregate>(partitionId, aggregateId, date, cancellationToken);
    var events = GetEventStream<TAggregate>(partitionId, aggregateId, date, snapshot?.Index ?? -1, cancellationToken);

    var aggregate = new TAggregate { PartitionId = partitionId, Id = aggregateId };
    await aggregate.RehydrateAsync(snapshot, events, cancellationToken);
    return aggregate.Version == 0 ? null : aggregate;
  }

  /// <inheritdoc />
  public async Task PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    var transaction = CreateTransaction(aggregate.PartitionId);
    await transaction.AddAggregateAsync(aggregate, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
  }

  /// <inheritdoc />
  public virtual IAggregateTransaction CreateTransaction(Guid partitionId) => new AggregateTransaction(Store.CreateTransaction(partitionId));

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
  protected virtual async Task<Snapshot<TAggregate>?> GetLatestSnapshotAsync<TAggregate>(
    Guid partitionId, Guid aggregateId, DateTimeOffset date, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    // Save performance by only filtering the necessary columns
    Expression<Func<Snapshot, bool>> filter = date == DateTimeOffset.MaxValue
      ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId
      : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date;

    return await Store
      .GetSnapshots<TAggregate>()
      .Where(filter)
      .OrderByDescending(x => x.Index)
      .AsAsyncEnumerable()
      .Cast<Snapshot<TAggregate>>()
      .FirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Get <see cref="Event"/> stream before or on <paramref name="date"/>
  /// </summary>
  /// <param name="partitionId"><see cref="Event"/>.<see cref="Record.PartitionId"/> to query</param>
  /// <param name="aggregateId"><see cref="Event"/>.<see cref="Record.AggregateId"/> to query</param>
  /// <param name="date">latest <see cref="DateTimeOffset"/> to query</param>
  /// <param name="after"><see cref="Event"/>.<see cref="Event.Index"/> should be greater then (NOT equal to) <paramref name="after"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <returns><see cref="IAsyncEnumerable{T}"/> of <see cref="Event"/></returns>
  protected virtual IAsyncEnumerable<Event<TAggregate>> GetEventStream<TAggregate>(
    Guid partitionId, Guid aggregateId, DateTimeOffset date, long after, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    // Save performance by only filtering the necessary columns
    Expression<Func<Event, bool>> filter = date == DateTimeOffset.MaxValue
      ? after == -1
        ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId
        : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Index > after
      : after == -1
        ? x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date
        : x => x.PartitionId == partitionId && x.AggregateId == aggregateId && x.Timestamp <= date && x.Index > after;

    return Store
      .GetEvents<TAggregate>()
      .Where(filter)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable()
      .Cast<Event<TAggregate>>();
  }
}