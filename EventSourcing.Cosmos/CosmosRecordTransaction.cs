using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Cosmos;

/// <summary>
/// ACID <see cref="CosmosRecordTransaction"/> of <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
public class CosmosRecordTransaction : IRecordTransaction
{
  private enum CosmosEventTransactionAction
  {
    ReadEvent,
    CreateEvent,
    CreateSnapshot,
    CreateProjection,
    DeleteEvent,
    DeleteSnapshot,
    DeleteProjection
  }
  
  private readonly TransactionalBatch _batch;
  private readonly List<(CosmosEventTransactionAction, dynamic)> _actions = new();

  /// <inheritdoc />
  public Guid PartitionId { get; }
  
  /// <summary>
  /// Create <see cref="CosmosRecordTransaction"/>
  /// </summary>
  /// <param name="container"><see cref="Container"/></param>
  /// <param name="partitionId">Partition Id</param>
  public CosmosRecordTransaction(Container container, Guid partitionId)
  {
    PartitionId = partitionId;
    _batch = container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
  }

  /// <inheritdoc />
  public IRecordTransaction AddEvents(IList<Event> events)
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);

    if (events.Count == 0) return this;

    var first = events.First();

    if (events.First().Index != 0)
    {
      // Check if the event before the current event is present in the Database
      // If not, this could be due to user error or the events being deleted during this transaction
      var check = new Event
      {
        PartitionId = first.PartitionId,
        AggregateId = first.AggregateId,
        Index = first.Index - 1
      };
      
      _batch.ReadItem(check.id, CosmosRecordStore.BatchItemRequestOptions);
      _actions.Add((CosmosEventTransactionAction.ReadEvent, check));
    }
    
    foreach (var e in events)
    {
      _batch.CreateItem(e, CosmosRecordStore.BatchItemRequestOptions);
      _actions.Add((CosmosEventTransactionAction.CreateEvent, e));
    }

    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction AddSnapshot(Snapshot snapshot)
  {
    RecordValidation.ValidateSnapshot(PartitionId, snapshot);
    _batch.CreateItem(snapshot, CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.CreateSnapshot, snapshot));

    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction UpsertProjection(Projection projection)
  {
    _batch.UpsertItem(projection, CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.CreateProjection, projection));

    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteAllEvents<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new()
  {
    var reservation = new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      Index = index + 1,
      Type = CosmosRecordStore.ReservationToken,
      AggregateType = CosmosRecordStore.ReservationToken
    };
    
    // Create Reservation to ensure Event with Index = index + 1 does not exist
    _batch.CreateItem(reservation, CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.CreateEvent, reservation));
    
    // Delete All Events
    for (var i = 0; i <= index; i++)
    {
      var deletion = new Event { PartitionId = PartitionId, AggregateId = aggregateId, Index = i };
      _batch.DeleteItem(deletion.id, CosmosRecordStore.BatchItemRequestOptions);
      _actions.Add((CosmosEventTransactionAction.DeleteEvent, deletion));
    }

    _batch.DeleteItem(reservation.id, CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.DeleteEvent, reservation));
    
    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteSnapshot<TAggregate>(Guid aggregateId, long index) where TAggregate : Aggregate, new()
  {
    var snapshot = new Snapshot { PartitionId = PartitionId, AggregateId = aggregateId, Index = index };
    _batch.DeleteItem(snapshot.id, CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.DeleteSnapshot, snapshot));

    return this;
  }

  /// <inheritdoc />
  public IRecordTransaction DeleteProjection<TProjection>(Guid aggregateId) where TProjection : Projection
  {
    _batch.DeleteItem($"{RecordKind.Projection}|{typeof(TProjection).Name}|{aggregateId}", CosmosRecordStore.BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.DeleteProjection, new object()));

    return this;
  }

  /// <inheritdoc />
  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    switch (_actions.Count)
    {
      case 0:
        return; // Nothing to Commit
      case > CosmosRecordStore.MaxTransactionSize:
        throw new RecordStoreException(
          $"Failed to commit {_actions.Count} items in {nameof(CosmosRecordTransaction)}. " +
          $"Currently CosmosDB has a limit of {CosmosRecordStore.MaxTransactionSize} items per transaction. " +
          "See https://docs.microsoft.com/en-us/azure/cosmos-db/sql/transactional-batch for more information. ");
    }

    try
    {
      var response = await _batch.ExecuteAsync(cancellationToken);
      if (!response.IsSuccessStatusCode) ThrowException(response);
    }
    catch (CosmosException e)
    {
      throw new RecordStoreException(e.Message, e);
    }
  }

  private void ThrowException(TransactionalBatchResponse response)
  {
    var inner = CosmosExceptionHelpers.CreateCosmosException(response);
    
    // TODO: Catch more known cases
    var exceptions = response
      .Zip(_actions)
      .Where(x => x.First.StatusCode != HttpStatusCode.FailedDependency)
      .Select(x => new { x.First.StatusCode, Action = x.Second.Item1, Event = x.Second.Item2 })
      .Select(x => x.StatusCode switch
      {
        HttpStatusCode.Conflict when x.Action == CosmosEventTransactionAction.CreateEvent =>
          $"Conflict while adding Event in {nameof(CosmosRecordTransaction)}. " +
          $"Adding {x.Event} failed, because an Event with Index {x.Event.Index} was already present in the {nameof(CosmosRecordStore)}.",
        HttpStatusCode.NotFound when x.Action == CosmosEventTransactionAction.ReadEvent =>
          $"Nonconsecutive Events Found while adding events for Aggregate with Id {x.Event.AggregateId}. " +
          $"Event with Index {x.Event.Index} not found while adding Event with Index {x.Event.Index+1} in {nameof(CosmosRecordTransaction)}.",
        _ => $"Exception during {x.Action.ToString()} action on {x.Event}: {(int)x.StatusCode} {x.StatusCode.ToString()}. See inner exception for details."
      })
      .Select(message => new RecordStoreException(message, inner))
      .ToList();

    throw exceptions.Count switch
    {
      0 => new RecordStoreException(
        $"Exception occurred while committing {nameof(CosmosRecordTransaction)}. " +
        $"Transaction failed with Status {response.StatusCode}. See inner exception for details", inner),
      1 => exceptions.Single(),
      _ => new AggregateException(exceptions)
    };
  }
}