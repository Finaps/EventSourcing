using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Record Store: Cosmos Connection for Querying and Storing <see cref="Record"/>s
/// </summary>
public class CosmosRecordStore : IRecordStore
{
  internal static readonly ItemRequestOptions ItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  internal static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  private const int MaxTransactionSize = 100;
  private const string ReservationToken = "<RESERVED>";
  
  private readonly Container _container;

  /// <summary>
  /// Initialize Cosmos Event Store
  /// </summary>
  /// <param name="options">Cosmos Event Store Options</param>
  /// <exception cref="ArgumentException"></exception>
  public CosmosRecordStore(IOptions<CosmosEventStoreOptions> options)
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
    .Where(x => x.Kind == RecordKind.View);

  public IQueryable<TView> GetViews<TView>() where TView : View, new() => _container
    .AsCosmosAsyncQueryable<TView>()
    .Where(x => x.Kind == RecordKind.View && x.Type == new TView().Type);

  public async Task<TView> GetViewByIdAsync<TView>(Guid partitionId, Guid aggregateId) where TView : View, new() =>
    await _container.ReadItemAsync<TView>(aggregateId.ToString(), new PartitionKey(partitionId.ToString()));

  public async Task AddEventsAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;

    await CreateTransaction(events.First().PartitionId)
      .AddEvents(events)
      .CommitAsync(cancellationToken);
  }

  public async Task AddSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default) =>
    await CreateTransaction(snapshot.PartitionId)
      .AddSnapshot(snapshot)
      .CommitAsync(cancellationToken);

  public async Task AddViewAsync(View view, CancellationToken cancellationToken = default) =>
    await CreateTransaction(view.PartitionId)
      .AddView(view)
      .CommitAsync(cancellationToken);

  public async Task DeleteEventsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var partitionKey = new PartitionKey(partitionId.ToString());

    // Get existing Event Indices
    var eventIndices = await Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderBy(index => index)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);

    if (!eventIndices.Any())
      throw new RecordStoreException($"Couldn't Delete Records with PartitionId '{partitionId}' and AggregateId '{aggregateId}': No Events found");

    var reservation = new Event
    {
      PartitionId = partitionId,
      AggregateId = aggregateId,
      Index = eventIndices.Last() + 1,
      Type = ReservationToken,
      AggregateType = ReservationToken
    };

    // Create Reservation on Event Stream
    var response = await _container.CreateItemAsync(reservation, partitionKey, ItemRequestOptions, cancellationToken);

    if (response.StatusCode != HttpStatusCode.Created)
      throw new RecordStoreException("Couldn't create reservation while deleting Events");
    
    // Delete Events
    foreach (var indices in eventIndices.Chunk(MaxTransactionSize))
    {
      var batch = _container.CreateTransactionalBatch(partitionKey);
      foreach (var index in indices)
        batch.DeleteItem(new Event { PartitionId = partitionId, AggregateId = aggregateId, Index = index }.id, BatchItemRequestOptions);
      await batch.ExecuteAsync(cancellationToken);
    }

    // Delete Reservation
    await _container.DeleteItemStreamAsync(reservation.id, partitionKey, ItemRequestOptions, cancellationToken);
  }

  public async Task DeleteEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteEventsAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task DeleteSnapshotsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    // Get Existing Snapshot Indices
    var snapshotIndices = await Snapshots
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderBy(index => index)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);
    
    // Delete Snapshots
    foreach (var indices in snapshotIndices.Chunk(MaxTransactionSize))
    {
      var batch = _container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
      foreach (var index in indices)
        batch.DeleteItem(new Snapshot { PartitionId = partitionId, AggregateId = aggregateId, Index = index }.id, BatchItemRequestOptions);
      await batch.ExecuteAsync(cancellationToken);
    }
  }

  public async Task DeleteSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteSnapshotsAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task DeleteViewsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    // Get Existing View Types
    var types = await Views
      .Where(x => x.PartitionId == partitionId && x.Id == aggregateId)
      .Select(x => x.Type)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);
    
    // Delete Views
    var batch = _container.CreateTransactionalBatch(new PartitionKey(partitionId.ToString()));
    foreach (var t in types)
      batch.DeleteItem(new View { PartitionId = partitionId, Id = aggregateId, Type = t }.id, BatchItemRequestOptions);
    await batch.ExecuteAsync(cancellationToken);
  }

  public async Task DeleteViewsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteSnapshotsAsync(Guid.Empty, aggregateId, cancellationToken);

  public IRecordTransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  public IRecordTransaction CreateTransaction(Guid partitionId) =>
    new CosmosRecordTransaction(_container, partitionId);
}