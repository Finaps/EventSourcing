using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core;

public interface IAggregateTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  /// <summary>
  /// Persist <see cref="Aggregate"/> to the <see cref="IAggregateTransaction{TBaseEvent}"/>
  /// </summary>
  /// <remarks>
  /// When all <see cref="Aggregate"/>s have been added, call <see cref="CommitAsync"/> to commit them
  /// </remarks>
  /// <param name="aggregate"><see cref="Aggregate"/> to persist</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate">Type of <see cref="Aggregate"/></typeparam>
  /// <returns>Persisted <see cref="Aggregate"/></returns>
  Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new();
  
  /// <summary>
  /// Commit <see cref="Aggregate"/>s to the <see cref="IEventStore{TBaseEvent}"/>
  /// </summary>
  /// <exception cref="ConcurrencyException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added <see cref="Aggregate"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <returns></returns>
  Task CommitAsync(CancellationToken cancellationToken = default);
}