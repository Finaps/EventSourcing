using EventSourcing.Core;
using EventSourcing.Core.Records;
using EventSourcing.Core.Services;

namespace EventSourcing.Cosmos;

public class CosmosTransaction : ITransaction
{
  private const string CheckEventAggregateType = "<CHECK>";
  
  private enum CosmosEventTransactionAction
  {
    ReadEvent,
    CreateEvent,
    CreateSnapshot,
    CreateAggregate
  }
  
  private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  private readonly TransactionalBatch _batch;
  private readonly List<(CosmosEventTransactionAction, dynamic)> _actions = new();

  public Guid PartitionId { get; }
  
  public CosmosTransaction(Container container, Guid partitionId)
  {
    PartitionId = partitionId;
    _batch = container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
  }

  public ITransaction Add(IList<Event> events)
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
      
      _batch.ReadItem(check.id, BatchItemRequestOptions);
      _actions.Add((CosmosEventTransactionAction.ReadEvent, check));
    }
    
    foreach (var e in events)
    {
      _batch.CreateItem(e, BatchItemRequestOptions);
      _actions.Add((CosmosEventTransactionAction.CreateEvent, e));
    }

    return this;
  }

  public ITransaction Add(Snapshot snapshot)
  {
    RecordValidation.ValidateSnapshot(PartitionId, snapshot);
    _batch.CreateItem(snapshot, BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.CreateSnapshot, snapshot));

    return this;
  }

  public ITransaction Add(Aggregate aggregate)
  {
    _batch.UpsertItem(aggregate, BatchItemRequestOptions);
    _actions.Add((CosmosEventTransactionAction.CreateAggregate, aggregate));

    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    if (_actions.Count == 0) return;

    var response = await _batch.ExecuteAsync(cancellationToken);
    
    if (!response.IsSuccessStatusCode) ThrowException(response);
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
        HttpStatusCode.Conflict when x.Action == CosmosEventTransactionAction.CreateEvent =>
          $"Conflict while adding Event in {nameof(CosmosTransaction)}. " +
          $"Adding {x.Event} failed, because an Event with Index {x.Event.Index} was already present in the {nameof(CosmosEventStore)}.",
        HttpStatusCode.NotFound when x.Action == CosmosEventTransactionAction.ReadEvent =>
          $"Nonconsecutive Events Found while adding events for Aggregate with Id {x.Event.AggregateId}. " +
          $"Event with Index {x.Event.Index} not found while adding Event with Index {x.Event.Index+1} in {nameof(CosmosTransaction)}.",
        _ => $"Exception during {x.Action.ToString()} action on {x.Event}: {(int)x.StatusCode} {x.StatusCode.ToString()}. See inner exception for details."
      })
      .Select(message => new EventStoreException(message, inner))
      .ToList();

    throw exceptions.Count switch
    {
      0 => new EventStoreException(
        $"Exception occurred while committing {nameof(CosmosTransaction)}. " +
        $"Transaction failed with Status {response.StatusCode}. See inner exception for details", inner),
      1 => exceptions.Single(),
      _ => new AggregateException(exceptions)
    };
  }
}