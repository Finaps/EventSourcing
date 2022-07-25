using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace Finaps.EventSourcing.EF;

/// <summary>
/// ACID <see cref="EntityFrameworkRecordTransaction"/> of <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
public class EntityFrameworkRecordTransaction : IRecordTransaction
{
  private record TransactionAction;

  private record AddEventsAction(IReadOnlyCollection<Event> Events) : TransactionAction;

  private record AddSnapshotAction(Snapshot Snapshot) : TransactionAction;

  private record UpsertProjectionAction(Projection Projection) : TransactionAction;

  private record DeleteAllEventsAction(Event Event) : TransactionAction;

  private record DeleteSnapshotAction(Snapshot Snapshot) : TransactionAction;

  private record DeleteProjectionAction(Type Type, Guid PartitionId, Guid AggregateId) : TransactionAction;

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
  public IRecordTransaction AddEvents<TAggregate>(IReadOnlyCollection<Event<TAggregate>> events)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);
    _actions.Add(new AddEventsAction(events));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction AddSnapshot<TAggregate>(Snapshot<TAggregate> snapshot)
    where TAggregate : Aggregate<TAggregate>, new()
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
    where TAggregate : Aggregate<TAggregate>, new()
  {
    _actions.Add(new DeleteAllEventsAction(new Event<TAggregate>
      { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteSnapshot<TAggregate>(Guid aggregateId, long index)
    where TAggregate : Aggregate<TAggregate>, new()
  {
    _actions.Add(new DeleteSnapshotAction(new Snapshot<TAggregate>
      { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteProjection<TProjection>(Guid aggregateId) where TProjection : Projection
  {
    _actions.Add(new DeleteProjectionAction(typeof(TProjection), PartitionId, aggregateId));
    return this;
  }

  /// <inheritdoc />
  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      // If no current transaction exists, wrap commit in new transaction
      if (_store.Context.Database.CurrentTransaction == null)
      {
        await using var transaction = await _store.Context.Database.BeginTransactionAsync(cancellationToken);
        await CommitAsync(_store.Context, _actions, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
      }
      else
      {
        await CommitAsync(_store.Context, _actions, cancellationToken);
      }
    }
    catch (DbUpdateException e)
    {
      throw new RecordStoreException(e.Message, e);
    }
  }

  private static async Task CommitAsync(RecordContext context, IEnumerable<TransactionAction> actions, CancellationToken cancellationToken = default)
  {
    foreach (var action in actions)
    {
      switch (action)
      {
        case AddEventsAction(var events):
          context.AddRange(events);
          break;

        case AddSnapshotAction(var snapshot):
          context.Add(snapshot);
          break;

        case UpsertProjectionAction(var projection):
          // Since EF Core has no Upsert functionality, we have to first query the original Projection :(
          var existing = await context.FindAsync(
            Cache.GetProjectionBaseType(projection.GetType()), 
            projection.PartitionId, projection.AggregateId);

          // Remove instead of update: this fixes problems with owned entities
          if (existing != null) context.Remove(existing);

          context.Add(projection);

          break;

        case DeleteAllEventsAction(var e):
          await context.DeleteWhereAsync($"{e.AggregateType}{nameof(Event)}s", e.PartitionId, e.AggregateId,
            cancellationToken);
          break;

        case DeleteSnapshotAction(var snapshot):
          context.Attach(snapshot);
          context.Remove(snapshot);
          break;

        case DeleteProjectionAction(var type, var partitionId, var aggregateId):
          await context.DeleteWhereAsync($"{type.Name}", partitionId, aggregateId, cancellationToken);
          break;
      }
      
      await context.SaveChangesAsync(cancellationToken);
    }
  }
}