namespace Finaps.EventSourcing.Core;

/// <summary>
/// ACID <see cref="IRecordTransaction"/> of <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
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
  /// Upsert <see cref="Projection"/>
  /// </summary>
  /// <param name="projection"><see cref="Projection"/></param>
  /// <returns></returns>
  IRecordTransaction UpsertProjection(Projection projection);

  /// <summary>
  /// Delete all <see cref="Event"/>s of a particular <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregateId"><see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Id"/></param>
  /// <param name="index"><see cref="Event.Index"/> of last <see cref="Event"/> (a.k.a. <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Version"/> - 1)</param>
  /// <remarks>
  /// For a more convenient method, refer to the async delete methods in <see cref="IRecordStore"/>
  /// </remarks>
  /// <returns></returns>
  IRecordTransaction DeleteAllEvents<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new();
  
  /// <summary>
  /// Delete <see cref="Snapshot"/> at a particular <see cref="Snapshot.Index"/> 
  /// </summary>
  /// <param name="aggregateId"><see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Id"/></param>
  /// <param name="index"><see cref="Snapshot"/>.<see cref="Snapshot.Index"/> to delete</param>
  /// <returns></returns>
  IRecordTransaction DeleteSnapshot<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new();
  
  /// <summary>
  /// Delete <see cref="Projection"/>
  /// </summary>
  /// <param name="aggregateId"><see cref="Projection"/>.<see cref="Projection.AggregateId"/></param>
  /// <param name="type"><see cref="Projection"/>.<see cref="Projection.Type"/> to delete</param>
  /// <returns></returns>
  IRecordTransaction DeleteProjection<TProjection>(Guid aggregateId) where TProjection : Projection, new();

  /// <summary>
  /// Commit ACID Record Transaction
  /// </summary>
  /// <exception cref="RecordStoreException">
  /// Thrown when a conflict occurs when commiting transaction,
  /// in which case none of the added or deleted <see cref="Event"/>s will be committed
  /// </exception>
  /// <param name="cancellationToken">Cancellation Token</param>
  Task CommitAsync(CancellationToken cancellationToken = default);
}
