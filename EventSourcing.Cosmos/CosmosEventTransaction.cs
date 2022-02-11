using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosEventTransaction : IEventTransaction
{
  private const string CheckEventAggregateType = "<CHECK>";
  
  private enum CosmosEventTransactionAction
  {
    Read,
    Create,
    Delete
  }
  
  private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  
  private readonly TransactionalBatch _batch;
  private readonly List<(CosmosEventTransactionAction, Event)> _actions = new();

  public Guid PartitionId { get; }
  
  public CosmosEventTransaction(Container container, Guid partitionId)
  {
    PartitionId = partitionId;
    _batch = container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
  }

  public IEventTransaction Add(IList<Event> events)
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);

    if (events.Count == 0) return this;

    var first = events.First();

    if (events.First().Index != 0)
      // Check if the event before the current event is present in the Database
      // If not, this could be due to user error or the events being deleted during this transaction
      ReadEvent(first.AggregateId, first.Index - 1);

    foreach (var e in events) CreateEvent(e);

    return this;
  }

  public IEventTransaction Delete(Guid aggregateId, long aggregateVersion)
  {
    for (long i = 0; i < aggregateVersion; i++)
     DeleteEvent(aggregateId, i);
    
    // Create and Delete Event with AggregateVersion to check if no events were added concurrently
    var check = new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      Index = aggregateVersion,
      AggregateType = CheckEventAggregateType,
    };
    
    CreateEvent(check); // If events were added concurrently, this will cause a concurrency exception
    DeleteEvent(aggregateId, aggregateVersion); // This will clean up the 'CHECK' event

    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    if (_actions.Count == 0) return;

    var response = await _batch.ExecuteAsync(cancellationToken);
    
    if (!response.IsSuccessStatusCode) ThrowException(response);
  }

  private void CreateEvent(Event e)
  {
    _batch.CreateItem(e, BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.Create, e));
  }

  private void DeleteEvent(Guid aggregateId, long index)
  {
    _batch.DeleteItem(Record.GetId(aggregateId, index), BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.Delete, new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      Index = index
    }));
  }
  
  private void ReadEvent(Guid aggregateId, long index)
  {
    _batch.ReadItem(Record.GetId(aggregateId, index), BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.Read, new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      Index = index
    }));
  }

  private void ThrowException(TransactionalBatchResponse response)
  {
    var inner = CosmosExceptionHelpers.CreateCosmosException(response);
    
    var exceptions = response
      .Zip(_actions)
      .Where(x => x.First.StatusCode != HttpStatusCode.FailedDependency)
      .Select(x => new { x.First.StatusCode, Action = x.Second.Item1, Event = x.Second.Item2 })
      .Select(x => x.StatusCode switch
      {
        HttpStatusCode.NotFound when x.Action == CosmosEventTransactionAction.Delete =>
          $"Event not found while deleting Event in {nameof(CosmosEventTransaction)}. " +
          $"Deleting Event with AggregateId '{x.Event.AggregateId}' and Index '{x.Event.Index}' failed, because it was not found in the {nameof(CosmosEventStore)}." +
          $"Either Aggregate with Id '{x.Event.AggregateId}' was concurrently deleted or {nameof(CosmosEventTransaction)}.{nameof(DeleteEvent)} was called with the correct 'aggregateVersion' argument.",
        HttpStatusCode.Conflict when x.Action == CosmosEventTransactionAction.Create && x.Event.AggregateType == CheckEventAggregateType =>
          $"Conflict while deleting Events in {nameof(CosmosEventTransaction)}. " +
          $"Deleting Events for Aggregate with Id '{x.Event.AggregateId}' and Version '{x.Event.Index}' failed, because an Event with Index {x.Event.Index} was found. " +
          $"Either Aggregate with Id '{x.Event.AggregateId}' was concurrently updated or {nameof(CosmosEventTransaction)}.{nameof(DeleteEvent)} was called with an incorrect 'aggregateVersion' argument.",
        HttpStatusCode.Conflict when x.Action == CosmosEventTransactionAction.Create =>
          $"Conflict while adding Event in {nameof(CosmosEventTransaction)}. " +
          $"Adding {x.Event.Format()} failed, because an Event with Index {x.Event.Index} was already present in the {nameof(CosmosEventStore)}.",
        HttpStatusCode.NotFound when x.Action == CosmosEventTransactionAction.Read =>
          $"Nonconsecutive Events Found while adding events for Aggregate with Id {x.Event.AggregateId}. " +
          $"Event with Index {x.Event.Index} not found while adding Event with Index {x.Event.Index+1} in {nameof(CosmosEventTransaction)}.",
        _ => $"Exception during {x.Action.ToString()} action on {x.Event}: {(int)x.StatusCode} {x.StatusCode.ToString()}. See inner exception for details."
      })
      .Select(message => new EventStoreException(message, inner))
      .ToList();

    throw exceptions.Count switch
    {
      0 => new EventStoreException(
        $"Exception occurred while committing {nameof(CosmosEventTransaction)}. " +
        $"Transaction failed with Status {response.StatusCode}. See inner exception for details", inner),
      1 => exceptions.Single(),
      _ => new AggregateException(exceptions)
    };
  }
}