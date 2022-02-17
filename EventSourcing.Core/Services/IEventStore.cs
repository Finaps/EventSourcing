using EventSourcing.Core.Records;

namespace EventSourcing.Core.Services;

/// <summary>
/// Event Store Interface: Persisting <see cref="Event"/>s to a Database
/// </summary>
public interface IEventStore
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
  /// Queryable and AsyncEnumerable Collection of <see cref="View{TAggregate}"/>s
  /// </summary>
  /// <remarks>
  /// To finalize the query, call <see cref="QueryableExtensions.AsAsyncEnumerable{T}"/>
  /// and use <c>System.Linq.Async</c>'s extension methods to get the results of your query.
  /// </remarks>
  IQueryable<TView> GetView<TView>() where TView : View, new();

  /// <summary>
  /// Store <see cref="Event"/>s to the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="ArgumentNullException">Thrown when <c>events</c> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="IndexedRecord.Index"/>s</exception>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Snapshot"/> to the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="snapshot"><see cref="Snapshot"/> to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddAsync(Snapshot snapshot, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Store <see cref="Aggregate"/> to the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="aggregate"><see cref="Aggregate"/> to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddAsync(Aggregate aggregate, CancellationToken cancellationToken = default);
    
  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="RecordStoreException">Thrown when conflicts occur when deleting <see cref="Event"/>s</exception>
  Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Create Event Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  ITransaction CreateTransaction(Guid partitionId);

  /// <summary>
  /// Create Event Transaction for <see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  ITransaction CreateTransaction();
}