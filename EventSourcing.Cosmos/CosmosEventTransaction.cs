using System.Reflection;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Cosmos;

public class CosmosEventTransaction<TBaseEvent> : IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };

  private readonly Container _container;
  private readonly Guid _partitionId;
  private readonly TransactionalBatch _batch;

  public CosmosEventTransaction(Container container, Guid partitionId)
  {
    _container = container;
    _partitionId = partitionId;
    _batch = container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
  }

  public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;

    await VerifyAsync(events, cancellationToken);
    
    foreach (var e in events) _batch.CreateItem(e, BatchItemRequestOptions);
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    var response = await _batch.ExecuteAsync(cancellationToken);
    if (!response.IsSuccessStatusCode) Throw(response);
  }
  
  private async Task<bool> ExistsAsync(Guid aggregateId, ulong version, CancellationToken cancellationToken = default)
  {
    var result = await _container.ReadItemStreamAsync(
      $"{aggregateId}|{version}",
      new PartitionKey(_partitionId.ToString()),
      cancellationToken: cancellationToken);
    return result.IsSuccessStatusCode;
  }

  private async Task VerifyAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    var tenantIds = events.Select(x => x.PartitionId).Distinct().ToList();
    
    if (tenantIds.Count > 1)
      throw new ArgumentException("All Events should have the same TenantId", nameof(events));
    
    if (tenantIds.Single() != _partitionId)
      throw new ArgumentException("All Events in a Transaction should have the same TenantId: " +
                                  $"expected: '{_partitionId}', found '{tenantIds.Single()}'", nameof(events));
    
    var aggregateIds = events.Select(x => x.AggregateId).Distinct().ToList();

    if (aggregateIds.Count > 1)
      throw new ArgumentException("All Events should have the same AggregateId", nameof(events));

    if (aggregateIds.Single() == Guid.Empty)
      throw new ArgumentException(
        "AggregateId should be set, did you forget to Add Events to an Aggregate?", nameof(events));

    if (!Utils.IsConsecutive(events.Select(e => e.AggregateVersion).ToList()))
      throw new InvalidOperationException("Event versions should be consecutive");

    if (events[0].AggregateVersion != 0 && !await ExistsAsync(events[0].AggregateId, events[0].AggregateVersion - 1, cancellationToken))
      throw new InvalidOperationException(
        $"Attempted to add nonconsecutive Event '{events[0].Type}' with Version {events[0].AggregateVersion} for Aggregate '{events[0].AggregateType}' with Id '{events[0].AggregateId}': " +
        $"no Event with Version {events[0].AggregateVersion - 1} exists");
  }
  
  private static void Throw(TransactionalBatchResponse response)
  {
    if (response.StatusCode != HttpStatusCode.Conflict)
      throw new EventStoreException(
        $"Encountered error while adding events: {(int)response.StatusCode} {response.StatusCode.ToString()}",
        CreateCosmosException(response));

    throw new ConcurrencyException("Encountered concurrency error while adding events", CreateCosmosException(response));
  }
  
  private static CosmosException CreateCosmosException(TransactionalBatchResponse response)
  {
    var subStatusCode = (int) response
      .GetType()
      .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
      .GetValue(response)!;
      
    return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
  }
}