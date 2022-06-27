using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace Finaps.EventSourcing.EF;

/// <summary>
/// ACID <see cref="EntityFrameworkRecordTransaction"/> of <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
public class EntityFrameworkRecordTransaction : IRecordTransaction
{
  private record TransactionAction;

  private record AddEventsAction(IList<Event> Events) : TransactionAction;

  private record AddSnapshotAction(Snapshot Snapshot) : TransactionAction;

  private record UpsertProjectionAction(Projection Projection) : TransactionAction;

  private record DeleteAllEventsAction(Event Event) : TransactionAction;

  private record DeleteSnapshotAction(Snapshot Snapshot) : TransactionAction;

  private record DeleteProjectionAction(Type Type, Guid AggregateId) : TransactionAction;

  private readonly List<TransactionAction> _actions = new();

  private readonly EntityFrameworkRecordStore _store;

  /// <inheritdoc />
  public Guid PartitionId { get; }

  /// <summary>
  /// Create <see cref="EntityFrameworkRecordTransaction"/>
  /// </summary>
  /// <param name="store"><see cref="EntityFrameworkRecordStore"/></param>
  /// <param name="partitionId">Unique Partition Identifier</param>
  public EntityFrameworkRecordTransaction(EntityFrameworkRecordStore store, Guid partitionId)
  {
    _store = store;
    PartitionId = partitionId;
  }

  /// <inheritdoc />
  public IRecordTransaction AddEvents(IList<Event> events)
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);
    _actions.Add(new AddEventsAction(events));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction AddSnapshot(Snapshot snapshot)
  {
    RecordValidation.ValidateSnapshot(PartitionId, snapshot);
    _actions.Add(new AddSnapshotAction(snapshot));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction UpsertProjection(Projection projection)
  {
    _actions.Add(new UpsertProjectionAction(projection));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteAllEvents<TAggregate>(Guid aggregateId, long index)
    where TAggregate : Aggregate, new()
  {
    _actions.Add(new DeleteAllEventsAction(new Event<TAggregate>
      { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteSnapshot<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new()
  {
    _actions.Add(new DeleteSnapshotAction(new Snapshot<TAggregate>
      { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteProjection<TProjection>(Guid aggregateId) where TProjection : Projection
  {
    _actions.Add(new DeleteProjectionAction(typeof(TProjection), aggregateId));
    return this;
  }

  /// <inheritdoc />
  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await using var transaction = await _store.Context.Database.BeginTransactionAsync(cancellationToken);

    foreach (var action in _actions)
    {
      switch (action)
      {
        case AddEventsAction(var events):
          _store.Context.AddRange(events);
          break;

        case AddSnapshotAction(var snapshot):
          _store.Context.Add(snapshot);
          break;

        case UpsertProjectionAction(var projection):
          // Since EF Core has no Upsert functionality, we have to first query the original Projection :(
          var existing = await _store.Context.FindAsync(
            projection.GetBaseType(), projection.PartitionId, projection.AggregateId);

          // Remove instead of update: this fixes problems with owned entities
          if (existing != null) _store.Context.Remove(existing);

          _store.Context.Add(projection);

          break;

        case DeleteAllEventsAction(var e):
          await _store.Context.DeleteWhereAsync($"{e.AggregateType}{nameof(Event)}s", PartitionId, e.AggregateId,
            cancellationToken);
          break;

        case DeleteSnapshotAction(var snapshot):
          _store.Context.Attach(snapshot);
          _store.Context.Remove(snapshot);
          break;

        case DeleteProjectionAction(var type, var aggregateId):
          await _store.Context.DeleteWhereAsync($"{type.Name}", PartitionId, aggregateId, cancellationToken);
          break;
      }
    }

    try
    {
      await _store.Context.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
    }
    catch (DbUpdateException e)
    {
      throw new RecordStoreException(e.Message, e);
    }
  }
}