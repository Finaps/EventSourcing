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
  
  internal const int MaxTransactionSize = 100;
  internal const string ReservationToken = "<RESERVED>";
  
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

  public async Task<TView> GetViewByIdAsync<TView>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TView : View, new()
  {
    try
    {
      return await _container.ReadItemAsync<TView>(
        new TView { PartitionId = partitionId, AggregateId = aggregateId }.id,
        new PartitionKey(partitionId.ToString()),
        cancellationToken: cancellationToken
      );
    }
    catch (CosmosException e)
    {
      throw new RecordStoreException(
        $"Exception occurred while calling {nameof(GetViewByIdAsync)}<{nameof(TView)}>. " +
                $"Read failed with Status {e.StatusCode}. See inner exception for details", e);
    }
  }
  
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

  public async Task DeleteAllEventsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    // Get existing Event Indices
    var index = await Events
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .AsAsyncEnumerable()
      .MaxAsync(cancellationToken);
    
    await CreateTransaction(partitionId)
      .DeleteAllEvents(aggregateId, index)
      .CommitAsync(cancellationToken);
  }

  public async Task DeleteAllEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllEventsAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task DeleteAllSnapshotsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
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
      var transaction = CreateTransaction(partitionId);
      foreach (var index in indices)
        transaction.DeleteSnapshot(aggregateId, index);
      await transaction.CommitAsync(cancellationToken);
    }
  }

  public async Task DeleteAllSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllSnapshotsAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task DeleteSnapshotAsync(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default) =>
    await CreateTransaction(partitionId)
      .DeleteSnapshot(aggregateId, index)
      .CommitAsync(cancellationToken);

  public async Task DeleteSnapshotAsync(Guid aggregateId, long index, CancellationToken cancellationToken = default) =>
    await DeleteSnapshotAsync(Guid.Empty, aggregateId, index, cancellationToken);

  public async Task DeleteAllViewsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    // Get Existing View Types
    var types = await Views
      .Where(x => x.PartitionId == partitionId && x.RecordId == aggregateId)
      .Select(x => x.Type)
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);

    foreach (var typeBatch in types.Chunk(MaxTransactionSize))
    {
      var transaction = CreateTransaction(partitionId);
      foreach (var t in typeBatch)
        transaction.DeleteView(aggregateId, t);
      await transaction.CommitAsync(cancellationToken);
    }
  }

  public async Task DeleteAllViewsAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAllViewsAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task DeleteViewAsync<TView>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TView : View, new() =>
    await CreateTransaction(partitionId)
      .DeleteView(aggregateId, new TView().Type)
      .CommitAsync(cancellationToken);

  public async Task DeleteViewAsync<TView>(Guid aggregateId, CancellationToken cancellationToken = default) where TView : View, new() =>
    await DeleteViewAsync<TView>(Guid.Empty, aggregateId, cancellationToken);

  public IRecordTransaction CreateTransaction() => CreateTransaction(Guid.Empty);

  public IRecordTransaction CreateTransaction(Guid partitionId) =>
    new CosmosRecordTransaction(_container, partitionId);
}