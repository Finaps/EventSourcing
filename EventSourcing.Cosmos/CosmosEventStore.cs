using EventSourcing.Core;
using EventSourcing.Core.Records;
using EventSourcing.Core.Services;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Event Store: Cosmos Connection for Querying and Storing <see cref="Event"/>s
/// </summary>
public class CosmosEventStore : IEventStore
{
  private const string ReservationToken = "<RESERVED>";
  
  private readonly Container _container;

  /// <summary>
  /// Initialize Cosmos Event Store
  /// </summary>
  /// <param name="options">Cosmos Event Store Options</param>
  /// <exception cref="ArgumentException"></exception>
  public CosmosEventStore(IOptions<CosmosEventStoreOptions> options)
  {
    const string baseError = "Error Constructing Cosmos Event Store. ";
    
    if (options.Value == null)
      throw new ArgumentException(baseError + $"{nameof(CosmosEventStoreOptions)} should not be null", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
      throw new ArgumentException(baseError + $"{nameof(CosmosEventStoreOptions)}.{nameof(CosmosEventStoreOptions.ConnectionString)} should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Database))
      throw new ArgumentException(baseError + $"{nameof(CosmosEventStoreOptions)}.{nameof(CosmosEventStoreOptions.Database)} should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Container))
      throw new ArgumentException(baseError + $"{nameof(CosmosEventStoreOptions)}.{nameof(CosmosEventStoreOptions.Container)} should not be empty", nameof(options));

    var clientOptions = new CosmosClientOptions { Serializer = new CosmosRecordSerializer(options.Value) };

    _container = new CosmosClient(options.Value.ConnectionString, clientOptions)
      .GetDatabase(options.Value!.Database)
      .GetContainer(options.Value.Container);
  }

  public IQueryable<Event> Events =>_container
    .AsCosmosAsyncQueryable<Event>()
    .Where(x => x.Kind == RecordKind.Event && x.Type != ReservationToken);

  public IQueryable<Snapshot> Snapshots => _container
    .AsCosmosAsyncQueryable<Snapshot>()
    .Where(x => x.Kind == RecordKind.Snapshot);

  public IQueryable<View> Views => _container
    .AsCosmosAsyncQueryable<View>()
    .Where(x => x.Kind == RecordKind.Aggregate);

  public IQueryable<TView> GetView<TView>() where TView : View, new() => _container
    .AsCosmosAsyncQueryable<TView>()
    .Where(x => x.Kind == RecordKind.Aggregate && x.Type == new TView().Type);

  public async Task<TView> GetViewAsync<TView>(Guid partitionId, Guid aggregateId) where TView : View, new() =>
    await _container.ReadItemAsync<TView>(aggregateId.ToString(), new PartitionKey(partitionId.ToString()));

  public async Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;

    await CreateTransaction(events.First().PartitionId)
      .Add(events)
      .CommitAsync(cancellationToken);
  }

  public async Task AddAsync(Snapshot snapshot, CancellationToken cancellationToken = default) =>
    await CreateTransaction(snapshot.PartitionId)
      .Add(snapshot)
      .CommitAsync(cancellationToken);

  public async Task AddAsync(Aggregate aggregate, CancellationToken cancellationToken = default) =>
    await CreateTransaction(aggregate.PartitionId)
      .Add(aggregate)
      .CommitAsync(cancellationToken);

  public async Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var partitionKey = new PartitionKey(partitionId.ToString());
    var options = new ItemRequestOptions { EnableContentResponseOnWrite = false };
    var batchOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false };
    const int maxTransactionSize = 100;
    
    // Get existing Event Indices
    var eventIndices = await Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderBy(index => index)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);

    if (!eventIndices.Any())
      throw new EventStoreException($"Couldn't Delete Records with PartitionId '{partitionId}' and AggregateId '{aggregateId}': No Events found");

    var reservation = new Event
    {
      PartitionId = partitionId,
      AggregateId = aggregateId,
      Index = eventIndices.Last() + 1,
      Type = ReservationToken,
      AggregateType = ReservationToken
    };

    // Create Reservation on Event Stream
    var response = await _container.CreateItemAsync(reservation, partitionKey, options, cancellationToken);

    if (response.StatusCode != HttpStatusCode.Created)
      throw new EventStoreException("Couldn't create reservation while deleting Events");

    // Delete Aggregate View
    await _container.DeleteItemStreamAsync(aggregateId.ToString(), partitionKey, options, cancellationToken);
    
    // Delete Events
    foreach (var indices in eventIndices.Chunk(maxTransactionSize))
    {
      var batch = _container.CreateTransactionalBatch(partitionKey);
      foreach (var index in indices)
        batch.DeleteItem(new Event { PartitionId = partitionId, AggregateId = aggregateId, Index = index }.id, batchOptions);
      await batch.ExecuteAsync(cancellationToken);
    }
    
    // Get Existing Snapshot Indices
    var snapshotIndices = await Snapshots
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderBy(index => index)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);
    
    // Delete Snapshots
    foreach (var indices in snapshotIndices.Chunk(maxTransactionSize))
    {
      var batch = _container.CreateTransactionalBatch(partitionKey);
      foreach (var index in indices)
        batch.DeleteItem(new Snapshot { PartitionId = partitionId, AggregateId = aggregateId, Index = index }.id, batchOptions);
      await batch.ExecuteAsync(cancellationToken);
    }
    
    // Delete Reservation
    await _container.DeleteItemStreamAsync(reservation.id, partitionKey, options, cancellationToken);
  }

  public async Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public ITransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  public ITransaction CreateTransaction(Guid partitionId) =>
    new CosmosTransaction(_container, partitionId);
}