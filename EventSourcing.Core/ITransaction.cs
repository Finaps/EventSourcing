namespace EventSourcing.Core;

public interface ITransaction
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
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with more than one unique <see cref="Event.PartitionId"/> or <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with <see cref="Guid.Empty"/> <see cref="Event.AggregateId"/></exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Record.Index"/>s</exception>
  ITransaction Add(IList<Event> events);
  ITransaction Add(Snapshot snapshot);
  ITransaction Add(Aggregate aggregate);

  /// <summary>
  /// Commit Transaction
  /// </summary>
  /// <exception cref="RecordStoreException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added or deleted <see cref="Event"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
