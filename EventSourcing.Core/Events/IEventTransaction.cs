using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core;

public interface IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  /// <summary>
  /// Add <see cref="Event"/>s to the <see cref="IEventTransaction{TBaseEvent}"/>
  /// </summary>
  /// <remarks>
  /// When all <see cref="Event"/>s have been added, call <see cref="CommitAsync"/> to commit them
  /// </remarks>
  /// <param name="events"><see cref="Event"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Commit <see cref="Event"/>s to the <see cref="IEventStore{TBaseEvent}"/>
  /// </summary>
  /// <exception cref="ConcurrencyException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added <see cref="Event"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <returns></returns>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
