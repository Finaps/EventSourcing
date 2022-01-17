namespace EventSourcing.Core;

public interface IEventTransaction
{
  /// <summary>
  /// Partition Id Transaction is scoped to
  /// </summary>
  Guid PartitionId { get; }
  
  /// <summary>
  /// Add <see cref="Event"/>s
  /// </summary>
  /// <remarks>
  /// When all <see cref="Event"/>s have been added, call <see cref="CommitAsync"/> to commit them
  /// </remarks>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="ArgumentNullException">Thrown when <c>events</c> is <c>null</c></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Record.Index"/>s</exception>
  Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete all <see cref="Event"/>s for a given <see cref="Aggregate"/>.<see cref="Aggregate.Id"/>
  /// </summary>
  /// <param name="aggregateId"><see cref="Aggregate"/>.<see cref="Aggregate.Id"/></param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Commit Transaction
  /// </summary>
  /// <exception cref="ConcurrencyException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added or deleted <see cref="Event"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
  /// <exception cref="ConcurrencyException">Thrown when concurrency conflicts occur when storing <see cref="Event"/>s</exception>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
