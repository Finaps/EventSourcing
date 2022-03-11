using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public class EntityFrameworkRecordTransaction : IRecordTransaction
{
  private record TransactionAction;
  private record AddEventsAction(IList<Event> Events) : TransactionAction;
  private record AddSnapshotAction(Snapshot Snapshot) : TransactionAction;
  private record UpsertProjectionAction(Projection Projection) : TransactionAction;
  private record DeleteAllEventsAction(Guid AggregateId) : TransactionAction;
  private record DeleteSnapshotAction(Guid AggregateId, long Index) : TransactionAction;
  private record DeleteProjectionAction(Guid AggregateId, string Type) : TransactionAction;

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

  public IRecordTransaction DeleteAllEvents(Guid aggregateId, long index)
  {
    _actions.Add(new DeleteAllEventsAction(aggregateId));
    return this;
  }

  public IRecordTransaction DeleteSnapshot(Guid aggregateId, long index)
  {
    _actions.Add(new DeleteSnapshotAction(aggregateId, index));
    return this;
  }

  public IRecordTransaction DeleteProjection(Guid aggregateId, string type)
  {
    _actions.Add(new DeleteProjectionAction(aggregateId, type));
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

          // If Previous Event.Index is not present, throw Error
          if (first.Index != 0 && await _store.Context.Set<EventEntity>().SingleOrDefaultAsync(x =>
                  x.PartitionId == PartitionId && x.AggregateId == first.AggregateId && x.Index == first.Index - 1, cancellationToken) == null)
            throw new RecordStoreException("Tried to add nonconsecutive Event");

          _store.Context.AddRange(events.Select(e => _store.Serializer.Serialize(e)));
          break;
        
        case AddSnapshotAction(var snapshot):
          _store.Context.Add(_store.Serializer.Serialize(snapshot));
          break;
        
        case UpsertProjectionAction(var projection):
          var existing = await _store.Context.FindAsync(
            projection.GetType(),
            projection.PartitionId, projection.AggregateId);

          if (existing != null)
            _store.Context.Remove(existing);
          
          _store.Context.Add(projection);
          break;
        
        case DeleteAllEventsAction(var aggregateId):
          await _store.Context.DeleteWhereAsync(nameof(EventEntity), PartitionId, aggregateId, cancellationToken);
          break;
        
        case DeleteSnapshotAction(var aggregateId, var index):
          var snapshotEntity = new SnapshotEntity { PartitionId = PartitionId, AggregateId = aggregateId, Index = index };
          _store.Context.Attach(snapshotEntity);
          _store.Context.Remove(snapshotEntity);
          break;
        
        case DeleteProjectionAction(var aggregateId, var type):
          await _store.Context.DeleteWhereAsync(type, PartitionId, aggregateId, cancellationToken);
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