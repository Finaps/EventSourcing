using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosEventTransaction : IEventTransaction
{
  private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  public Guid PartitionId { get; }
  
  private readonly TransactionalBatch _batch;

  private readonly HashSet<Guid> _addedAggregateIds = new();
  private readonly HashSet<Guid> _deletedAggregateIds = new();

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
      _batch.ReadItem(Record.GetId(first.AggregateId, first.Index - 1));

    foreach (var e in events)
      _batch.CreateItem(e, BatchItemRequestOptions);

    _addedAggregateIds.Add(first.AggregateId);

    return this;
  }

  public IEventTransaction Delete(Guid aggregateId, long aggregateVersion)
  {
    for (long i = 0; i < aggregateVersion; i++)
      _batch.DeleteItem(Record.GetId(aggregateId, i));
    
    // Create and Delete Event with AggregateVersion to check if no events were added concurrently
    var check = new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      AggregateType = "<CHECK>",
      Index = aggregateVersion
    };
    
    _batch.CreateItem(check);     // If events were added concurrently, this will cause a concurrency exception
    _batch.DeleteItem(check.id);  // This will clean up the 'check' event

    _deletedAggregateIds.Add(aggregateId);

    return this;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    if (!_addedAggregateIds.Any() && !_deletedAggregateIds.Any())
      return;

    if (_addedAggregateIds.Intersect(_deletedAggregateIds).Any())
      throw new InvalidOperationException("Cannot add and delete the same AggregateId in one transaction");

    var response = await _batch.ExecuteAsync(cancellationToken);
    if (!response.IsSuccessStatusCode) CosmosExceptionHelpers.Throw(response);
  }
}