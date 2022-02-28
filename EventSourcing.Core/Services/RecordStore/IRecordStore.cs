namespace EventSourcing.Core;

/// <summary>
/// Event Store Interface: Persisting <see cref="Event"/>s to a Database
/// </summary>
public interface IRecordStore
{
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Events"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  IQueryable<Event> Events { get; }
  
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Snapshot"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  IQueryable<Snapshot> Snapshots { get; }
  
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Projection"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query
  /// </remarks>
  IQueryable<Projection> Projections { get; }

  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Projection"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query.
  /// </remarks>
  IQueryable<TProjection> GetProjections<TProjection>() where TProjection : Projection, new();
  Task<TProjection> GetProjectionByIdAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new();
  async Task<TProjection> GetProjectionByIdAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new() =>
    await GetProjectionByIdAsync<TProjection>(Guid.Empty, aggregateId, cancellationToken);

  /// <summary>
  /// Store <see cref="Event"/>s to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="ArgumentNullException">Thrown when <c>events</c> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="IndexedRecord.Index"/>s</exception>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  Task AddEventsAsync(IList<Event> events, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Snapshot"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="snapshot"><see cref="Snapshot"/> to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Aggregate"/> to the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="projection"><see cref="Projection"/> to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddProjectionAsync(Projection projection, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Event"/>s</exception>
  Task DeleteAllEventsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAllEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task DeleteAllSnapshotsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAllSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Snapshot"/>s</exception>
  Task DeleteSnapshotAsync(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Snapshot"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index"><see cref="Snapshot"/> <see cref="Snapshot.Index"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteSnapshotAsync(Guid aggregateId, long index, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Projection"/>s</exception>
  Task DeleteAllProjectionsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAllProjectionsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Projection"/>s</exception>
  Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new();

  /// <summary>
  /// Delete <see cref="Projection"/>s for an <see cref="Aggregate"/> from the <see cref="IRecordStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteProjectionAsync<TProjection>(Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new();

  /// <summary>
  /// Create Event Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IRecordTransaction? CreateTransaction(Guid partitionId);

  /// <summary>
  /// Create Event Transaction for <see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IRecordTransaction? CreateTransaction();
}