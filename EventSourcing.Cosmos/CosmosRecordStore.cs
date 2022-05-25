using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Cosmos;

/// <summary>
/// Cosmos Record Store: Cosmos Connection for Querying and Storing <see cref="Record"/>s
/// </summary>
public class CosmosRecordStore : IRecordStore
{
  internal static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
  {
    EnableContentResponseOnWrite = false
  };
  
  internal const int MaxTransactionSize = 100;
  internal const string ReservationToken = "<RESERVED>";
  
  private readonly Container _container;
  private bool _isDeleteAggregateProcedureInitialized;
  private bool _isDeleteAllEventsProcedureInitialized;
  private bool _isDeleteAllSnapshotsProcedureInitialized;

  /// <summary>
  /// Initialize Cosmos Record Store
  /// </summary>
  /// <param name="options">Cosmos Record Store Options</param>
  /// <exception cref="ArgumentException"></exception>
  public CosmosRecordStore(IOptions<CosmosRecordStoreOptions> options)
  {
    const string baseError = "Error Constructing Cosmos Event Store. ";
    
    if (options.Value == null)
      throw new ArgumentException(baseError + $"{nameof(CosmosRecordStoreOptions)} should not be null", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
      throw new ArgumentException(baseError + $"{nameof(CosmosRecordStoreOptions)}.{nameof(CosmosRecordStoreOptions.ConnectionString)} should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Database))
      throw new ArgumentException(baseError + $"{nameof(CosmosRecordStoreOptions)}.{nameof(CosmosRecordStoreOptions.Database)} should not be empty", nameof(options));

    if (string.IsNullOrWhiteSpace(options.Value.Container))
      throw new ArgumentException(baseError + $"{nameof(CosmosRecordStoreOptions)}.{nameof(CosmosRecordStoreOptions.Container)} should not be empty", nameof(options));

    var clientOptions = new CosmosClientOptions { Serializer = new CosmosRecordSerializer(options.Value) };

    _container = new CosmosClient(options.Value.ConnectionString, clientOptions)
      .GetDatabase(options.Value!.Database)
      .GetContainer(options.Value.Container);
  }

  /// <inheritdoc />
  public IQueryable<Event> GetEvents<TAggregate>() where TAggregate : Aggregate, new() => _container
    .AsCosmosAsyncQueryable<Event>()
    .Where(x => x.AggregateType == typeof(TAggregate).Name && x.Kind == RecordKind.Event && x.Type != ReservationToken);

  /// <inheritdoc />
  public IQueryable<Snapshot> GetSnapshots<TAggregate>() where TAggregate : Aggregate, new() => _container
    .AsCosmosAsyncQueryable<Snapshot>()
    .Where(x => x.AggregateType == typeof(TAggregate).Name && x.Kind == RecordKind.Snapshot);

  /// <inheritdoc />
  public IQueryable<TProjection> GetProjections<TProjection>() where TProjection : Projection => _container
    .AsCosmosAsyncQueryable<TProjection>()
    .Where(x => x.Kind == RecordKind.Projection && x.Type == typeof(TProjection).Name);

  /// <inheritdoc />
  public async Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection
  {
    try
    {
      return await _container.ReadItemAsync<TProjection>(
        $"{RecordKind.Projection}|{typeof(TProjection).Name}|{aggregateId}",
        new PartitionKey(partitionId.ToString()),
        cancellationToken: cancellationToken
      );
    }
    catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
    {
      return null;
    }
    catch (CosmosException e)
    {
      throw new RecordStoreException(
        $"Exception occurred while calling {nameof(GetProjectionByIdAsync)}<{nameof(TProjection)}>. " +
        $"Read failed with Status {e.StatusCode}. See inner exception for details", e);
    }
  }

  /// <inheritdoc />
  public async Task AddEventsAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;

    await CreateTransaction(events.First().PartitionId)
      .AddEvents(events)
      .CommitAsync(cancellationToken);
  }

  /// <inheritdoc />
  public async Task AddSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default) =>
    await CreateTransaction(snapshot.PartitionId)
      .AddSnapshot(snapshot)
      .CommitAsync(cancellationToken);

  /// <inheritdoc />
  public async Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default) =>
    await CreateTransaction(projection.PartitionId)
      .UpsertProjection(projection)
      .CommitAsync(cancellationToken);

  /// <inheritdoc />
  public async Task<int> DeleteAllEventsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    if (!_isDeleteAllEventsProcedureInitialized)
    {
      await _container.CreateDeleteAllEventsProcedure();
      _isDeleteAllEventsProcedureInitialized = true;
    }

    return await _container.DeleteAllEvents(partitionId, aggregateId);
  }

  /// <inheritdoc />
  public async Task<int> DeleteAllSnapshotsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    if (!_isDeleteAllSnapshotsProcedureInitialized)
    {
      await _container.CreateDeleteAllSnapshotsProcedure();
      _isDeleteAllSnapshotsProcedureInitialized = true;
    }

    return await _container.DeleteAllSnapshots(partitionId, aggregateId);
  }

  /// <inheritdoc />
  public async Task DeleteSnapshotAsync<TAggregate>(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await CreateTransaction(partitionId)
      .DeleteSnapshot<TAggregate>(aggregateId, index)
      .CommitAsync(cancellationToken);

  /// <inheritdoc />
  public async Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection =>
    await CreateTransaction(partitionId)
      .DeleteProjection<TProjection>(aggregateId)
      .CommitAsync(cancellationToken);

  /// <inheritdoc />
  public async Task<int> DeleteAggregateAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    if (!_isDeleteAggregateProcedureInitialized)
    {
      await _container.CreateDeleteAggregateAllProcedure();
      _isDeleteAggregateProcedureInitialized = true;
    }

    return await _container.DeleteAggregateAll(partitionId, aggregateId);
  }

  /// <inheritdoc />
  public IRecordTransaction CreateTransaction(Guid partitionId) =>
    new CosmosRecordTransaction(_container, partitionId);
}