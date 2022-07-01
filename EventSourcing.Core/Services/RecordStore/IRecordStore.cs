namespace Finaps.EventSourcing.Core;

/// <summary>
/// <see cref="Record"/> store<see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
/// <seealso cref="AggregateService"/>
public interface IRecordStore
{
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Event"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  /// <seealso cref="Event"/>
  IQueryable<Event> GetEvents<TAggregate>() where TAggregate : Aggregate<TAggregate>, new();
  
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Snapshot"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  /// <seealso cref="Snapshot"/>
  IQueryable<Snapshot> GetSnapshots<TAggregate>() where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Projection"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query.
  /// </remarks>
  /// <typeparam name="TProjection"><see cref="Projection"/> type to query</typeparam>
  IQueryable<TProjection> GetProjections<TProjection>() where TProjection : Projection;
  
  /// <summary>
  /// Get <see cref="Projection"/> by <see cref="Record.PartitionId"/> and <see cref="Record.AggregateId"/>
  /// </summary>
  /// <param name="partitionId"><see cref="Projection"/>.<see cref="Record.PartitionId"/></param>
  /// <param name="aggregateId"><see cref="Projection"/>.<see cref="Record.AggregateId"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <returns><see cref="Projection"/> of type <typeparamref name="TProjection"/></returns>
  Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection;

  /// <summary>
  /// Store <see cref="Event"/>s to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Record.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Record.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Record.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Event.Index"/></exception>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  Task AddEventsAsync<TAggregate>(IReadOnlyCollection<Event<TAggregate>> events, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new();
  
  /// <summary>
  /// Store <see cref="Snapshot"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="snapshot"><see cref="Snapshot"/> to add</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  Task AddSnapshotAsync<TAggregate>(Snapshot<TAggregate> snapshot, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate<TAggregate>, new();
  
  /// <summary>
  /// Store <see cref="Projection"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="projection"><see cref="Projection"/> to upsert</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Event"/>s</exception>
  Task<int> DeleteAllEventsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task<int> DeleteAllSnapshotsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task DeleteSnapshotAsync<TAggregate>(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Projection"/>s</exception>
  Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection;

  /// <summary>
  /// Delete all <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s for an <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <returns>Number of items deleted from <see cref="IRecordStore"/></returns>
  Task<int> DeleteAggregateAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new();

  /// <summary>
  /// Create Record Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IRecordTransaction CreateTransaction(Guid partitionId);

  #region DefaultPartitionIdOverloads
  
  /// <summary>
  /// Get <see cref="Projection"/> by <see cref="Record.AggregateId"/> and default <see cref="Record.PartitionId"/>
  /// </summary>
  /// <param name="aggregateId"><see cref="Projection"/>.<see cref="Record.AggregateId"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <returns><see cref="Projection"/> of type <typeparamref name="TProjection"/></returns>
  async Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection =>
    await GetProjectionByIdAsync<TProjection>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task<int> DeleteAllEventsAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await DeleteAllEventsAsync<TAggregate>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task<int> DeleteAllSnapshotsAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await DeleteAllSnapshotsAsync<TAggregate>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteSnapshotAsync<TAggregate>(Guid aggregateId, long index, CancellationToken cancellationToken = default) where TAggregate : Aggregate<TAggregate>, new() =>
    await DeleteSnapshotAsync<TAggregate>(Guid.Empty, aggregateId, index, cancellationToken);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate{TAggregate}"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteProjectionAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default)
    where TProjection : Projection =>
    await DeleteProjectionAsync<TProjection>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Create Record Transaction for <see cref="Guid"/>.<see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IRecordTransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  #endregion
}