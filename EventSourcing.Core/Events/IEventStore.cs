namespace EventSourcing.Core;

/// <summary>
/// Event Store Interface: Persisting <see cref="Event"/>s to a Database
/// </summary>
public interface IEventStore
{
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Events"/>s
  /// </summary>
  /// <remarks>
  /// Implementations of this method should implement the <c cref="IQueryable{T}">IQueryable</c> and
  /// <c cref="IAsyncEnumerable{T}">IAsyncEnumerable</c> interfaces, such that the async extensions
  /// e.g. <c>System.Linq.Async</c> or <c>EventSourcing.Core.QueryableExtensions</c> work as intended.
  /// </remarks>
  IQueryable<Event> Events { get; }

  /// <summary>
  /// AddAsync: Store <see cref="Event"/>s to the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="ArgumentNullException">Thrown when <c>events</c> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Record.Index"/>s</exception>
  /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  /// <exception cref="ConcurrencyException">Thrown when concurrency conflicts occur when storing <see cref="Event"/>s</exception>
  Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="EventStoreException">Thrown when conflicts occur when deleting <see cref="Event"/>s</exception>
  /// <exception cref="ConcurrencyException">Thrown when concurrency conflicts occur when deleting <see cref="Event"/>s</exception>
  Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Get <see cref="Aggregate"/> Version 
  /// </summary>
  /// <param name="partitionId">Partition id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <returns>Aggregate Version</returns>
  Task<long> GetAggregateVersionAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Get <see cref="Aggregate"/> Version 
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <returns>Aggregate Version</returns>
  Task<long> GetAggregateVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Create Event Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IEventTransaction CreateTransaction(Guid partitionId);

  /// <summary>
  /// Create Event Transaction for <see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IEventTransaction CreateTransaction();
}