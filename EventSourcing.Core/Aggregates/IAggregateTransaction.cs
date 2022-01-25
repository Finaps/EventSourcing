namespace EventSourcing.Core;

public interface IAggregateTransaction
{
  /// <summary>
  /// Persist <see cref="Aggregate"/> to the <see cref="IAggregateTransaction"/>
  /// </summary>
  /// <remarks>
  /// When all <see cref="Aggregate"/>s have been added, call <see cref="CommitAsync"/> to commit them
  /// </remarks>
  /// <param name="aggregate"><see cref="Aggregate"/> to persist</param>
  /// <returns>Persisted <see cref="Aggregate"/></returns>
  IAggregateTransaction Add(Aggregate aggregate);
  
  IAggregateTransaction Delete(Guid aggregateId, long aggregateVersion);
  
  /// <summary>
  /// Commit <see cref="Aggregate"/>s to the <see cref="IEventStore"/>
  /// </summary>
  /// <exception cref="ConcurrencyException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added <see cref="Aggregate"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <returns></returns>
  Task CommitAsync(CancellationToken cancellationToken = default);
}