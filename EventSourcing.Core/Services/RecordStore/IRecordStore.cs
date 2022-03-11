namespace EventSourcing.Core;

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
  IQueryable<Event> Events { get; }
  
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Snapshot"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  /// <seealso cref="Snapshot"/>
  IQueryable<Snapshot> Snapshots { get; }

  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Projection"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query.
  /// </remarks>
  /// <typeparam name="TProjection"><see cref="Projection"/> type to query</typeparam>
  IQueryable<TProjection> GetProjections<TProjection>() where TProjection : Projection, new();
  
  /// <summary>
  /// Get <see cref="Projection"/> by <see cref="Projection.PartitionId"/> and <see cref="Projection.AggregateId"/>
  /// </summary>
  /// <param name="partitionId"><see cref="Projection"/>.<see cref="Projection.PartitionId"/></param>
  /// <param name="aggregateId"><see cref="Projection"/>.<see cref="Projection.AggregateId"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <returns><see cref="Projection"/> of type <see cref="TProjection"/></returns>
  Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new();

  /// <summary>
  /// Store <see cref="Event"/>s to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="ArgumentNullException">Thrown when <see cref="events"/> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Event.Index"/></exception>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  Task AddEventsAsync(IList<Event> events, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Snapshot"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="snapshot"><see cref="Snapshot"/> to add</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  Task AddSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Projection"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="projection"><see cref="Projection"/> to upsert</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Event"/>s</exception>
  Task DeleteAllEventsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task DeleteAllSnapshotsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task DeleteSnapshotAsync(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Projection"/>s</exception>
  Task DeleteAllProjectionsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Projection"/>s</exception>
  Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new();

  /// <summary>
  /// Delete all <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s for an <see cref="Aggregate"/>
  /// </summary>
  /// <returns>Number of items deleted from <see cref="IRecordStore"/></returns>
  Task<int> DeleteAggregateAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Create Record Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IRecordTransaction CreateTransaction(Guid partitionId);

  #region DefaultPartitionIdOverloads
  
  /// <summary>
  /// Get <see cref="Projection"/> by <see cref="Projection.AggregateId"/> and default <see cref="Projection.PartitionId"/>
  /// </summary>
  /// <param name="aggregateId"><see cref="Projection"/>.<see cref="Projection.AggregateId"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <returns><see cref="Projection"/> of type <see cref="TProjection"/></returns>
  async Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new() =>
    await GetProjectionByIdAsync<TProjection>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteAllEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllEventsAsync(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteAllSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllSnapshotsAsync(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteSnapshotAsync(Guid aggregateId, long index, CancellationToken cancellationToken = default) =>
    await DeleteSnapshotAsync(Guid.Empty, aggregateId, index, cancellationToken);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteAllProjectionsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllProjectionsAsync(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  async Task DeleteProjectionAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default)
    where TProjection : Projection, new() =>
    await DeleteProjectionAsync<TProjection>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Create Record Transaction for <see cref="Guid"/>.<see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IRecordTransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  #endregion
}