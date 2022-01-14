using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core;

public interface IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
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
  Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);

  /// <summary>
  /// Delete all <see cref="Event"/>s for a given <see cref="Aggregate"/>
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
  /// <returns></returns>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
