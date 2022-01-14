namespace EventSourcing.Core;

public interface IEventStore : IEventStore<Event> { }

/// <summary>
/// Event Store Interface: Persisting <see cref="Event"/>s to a Database
/// </summary>
/// <remarks>
/// The <c>TBaseEvent</c> type parameter determines which <see cref="Event"/> fields are queryable on a database Level
/// </remarks>
/// <typeparam name="TBaseEvent">Base <see cref="Event"/> for <see cref="IEventStore{TBaseEvent}"/></typeparam>
public interface IEventStore<TBaseEvent> where TBaseEvent : Event, new()
{
  /// <summary>
  /// Queryable and AsyncEnumerable Collection of <see cref="Events"/>s
  /// </summary>
  /// <remarks>
  /// Implementations of this method should implement the <c cref="IQueryable{T}">IQueryable</c> and
  /// <c cref="IAsyncEnumerable{T}">IAsyncEnumerable</c> interfaces, such that the async extensions
  /// e.g. <c>System.Linq.Async</c> or <c>EventSourcing.Core.QueryableExtensions</c> work as intended.
  /// </remarks>
  IQueryable<TBaseEvent> Events { get; }
    
  /// <summary>
  /// Add <see cref="Event"/>s to the <see cref="IEventStore{TBaseEvent}"/>
  /// </summary>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore{TBaseEvent}"/>
  /// </summary>
  /// <param name="partitionId">Aggregate Partition Id</param>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default);
  
    /// <summary>
  /// Delete <see cref="Event"/>s for an <see cref="Aggregate"/> from the <see cref="IEventStore{TBaseEvent}"/>
  /// </summary>
    /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Create Event Transaction
  /// </summary>
  /// <param name="partitionId">Transaction Partition identifier</param>
  /// <returns></returns>
  IEventTransaction<TBaseEvent> CreateTransaction(Guid partitionId);

  /// <summary>
  /// Create Event Transaction for <see cref="Guid.Empty"/> partition
  /// </summary>
  /// <returns></returns>
  IEventTransaction<TBaseEvent> CreateTransaction();
}