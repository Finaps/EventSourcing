using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public class EntityFrameworkRecordTransaction : IRecordTransaction
{
  private record TransactionAction;
  private record AddEventsAction(IList<Event> Events) : TransactionAction;
  private record AddSnapshotAction(Snapshot Snapshot) : TransactionAction;
  private record UpsertProjectionAction(Projection Projection) : TransactionAction;
  private record DeleteAllEventsAction(Event Event) : TransactionAction;
  private record DeleteSnapshotAction(Snapshot Snapshot) : TransactionAction;
  private record DeleteProjectionAction(Projection Projection) : TransactionAction;

  private readonly List<TransactionAction> _actions = new();

  private readonly EntityFrameworkRecordStore _store;
  public Guid PartitionId { get; }

  public EntityFrameworkRecordTransaction(EntityFrameworkRecordStore store, Guid partitionId)
  {
    _store = store;
    PartitionId = partitionId;
  }

  public IRecordTransaction AddEvents(IList<Event> events)
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);
    _actions.Add(new AddEventsAction(events));
    return this;
  }

  public IRecordTransaction AddSnapshot(Snapshot snapshot)
  {
    RecordValidation.ValidateSnapshot(PartitionId, snapshot);
    _actions.Add(new AddSnapshotAction(snapshot));
    return this;
  }

  public IRecordTransaction UpsertProjection(Projection projection)
  {
    _actions.Add(new UpsertProjectionAction(projection));
    return this;
  }

  public IRecordTransaction DeleteAllEvents<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new()
  {
    _actions.Add(new DeleteAllEventsAction(new Event<TAggregate> { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  public IRecordTransaction DeleteSnapshot<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new()
  {
    _actions.Add(new DeleteSnapshotAction(new Snapshot<TAggregate> { PartitionId = PartitionId, AggregateId = aggregateId, Index = index }));
    return this;
  }

  public IRecordTransaction DeleteProjection<TProjection>(Guid aggregateId) where TProjection : Projection, new()
  {
    _actions.Add(new DeleteProjectionAction(new TProjection { PartitionId = PartitionId, AggregateId = aggregateId }));
    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    await using var transaction = await _store.Context.Database.BeginTransactionAsync(cancellationToken);

    foreach (var action in _actions)
    {
      switch (action)
      {
        case AddEventsAction(var events):
          var first = events.First();

          if (first.Index != 0 && await _store.Context.FindAsync(
                _store.Context.Model.FindEntityType(first.GetType())?.GetRootType().ClrType, 
                first.PartitionId, first.AggregateId, first.Index - 1) == null)
            throw new RecordStoreException("Tried to add nonconsecutive Event");

          _store.Context.AddRange(events);
          break;
        
        case AddSnapshotAction(var snapshot):
          _store.Context.Add(snapshot);
          break;
        
        case UpsertProjectionAction(var projection):
          
          // Since EF Core has no Upsert functionality, we have to first query the original Projection :(
          var existing = await _store.Context.FindAsync(
            projection.GetType(),
            projection.PartitionId, projection.AggregateId);

          // Remove instead of update: this fixes problems with owned entities
          if (existing != null) _store.Context.Remove(existing);

          _store.Context.Add(projection);

          break;
        
        case DeleteAllEventsAction(var e):
          await _store.Context.DeleteWhereAsync(nameof(e), PartitionId, e.AggregateId, cancellationToken);
          break;
        
        case DeleteSnapshotAction(var snapshot):
          _store.Context.Attach(snapshot);
          _store.Context.Remove(snapshot);
          break;
        
        case DeleteProjectionAction(var projection):
          _store.Context.Attach(projection);
          _store.Context.Remove(projection);
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
      throw new RecordStoreException("Db Update Exception", e);
    }
  }
}