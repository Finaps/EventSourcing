namespace EventSourcing.Core;

public interface IRecordTransaction
{
  /// <summary>
  /// Partition Id <see cref="IRecordTransaction"/> is scoped to
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
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with nonconsecutive <see cref="Event.Index"/>s</exception>
  /// <returns>This <see cref="IRecordTransaction"/></returns>
  IRecordTransaction AddEvents(IList<Event> events);
  
  /// <summary>
  /// Add <see cref="Snapshot"/>
  /// </summary>
  /// <param name="snapshot"><see cref="Snapshot"/> to add</param>
  /// <returns>This <see cref="IRecordTransaction"/></returns>
  IRecordTransaction AddSnapshot(Snapshot snapshot);
  
  /// <summary>
  /// Add <see cref="View"/>
  /// </summary>
  /// <param name="view"><see cref="View"/></param>
  /// <returns></returns>
  IRecordTransaction AddView(View view);

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
