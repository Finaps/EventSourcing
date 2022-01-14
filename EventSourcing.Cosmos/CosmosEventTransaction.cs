using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosEventTransaction : IEventTransaction
{
  private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  public Guid PartitionId { get; }

  private readonly Container _container;
  private readonly TransactionalBatch _batch;

  private readonly HashSet<Guid> _addedAggregateIds = new();
  private readonly HashSet<Guid> _deletedAggregateIds = new();

  public CosmosEventTransaction(Container container, Guid partitionId)
  {
    PartitionId = partitionId;
    
    _container = container;
    _batch = container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
  }

  public Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    EventValidation.Validate(PartitionId, events);

    if (events.Count == 0) return Task.CompletedTask;

    var first = events.First();

    if (events.First().AggregateVersion != 0)
      // Check if the event before the current event is present in the Database
      // If not, this could be due to user error or the events being deleted during this transaction
      _batch.ReadItem(Event.GetId(first.AggregateId, first.AggregateVersion - 1));
    
    foreach (var e in events)
      _batch.CreateItem(e, BatchItemRequestOptions);

    _addedAggregateIds.Add(first.AggregateId);
    
    return Task.CompletedTask;
  }

  public async Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var aggregateVersion = await _container
      .AsCosmosAsyncQueryable<Event>()
      .Where(x => x.PartitionId == PartitionId && x.AggregateId == aggregateId)
      .Select(x => x.AggregateVersion)
      .OrderByDescending(version => version)
      .AsAsyncEnumerable()
      .FirstAsync(cancellationToken);

    for (long i = 0; i <= aggregateVersion; i++)
      _batch.DeleteItem(Event.GetId(aggregateId, i));
    
    // Create and Delete Event with AggregateVersion+1 to check if no events were added concurrently
    var check = new Event
    {
      PartitionId = PartitionId,
      AggregateId = aggregateId,
      AggregateVersion = aggregateVersion + 1
    };
    
    _batch.CreateItem(check);     // If events were added concurrently, this will cause a concurrency exception
    _batch.DeleteItem(check.id);  // This will clean up the 'check' event

    _deletedAggregateIds.Add(aggregateId);
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