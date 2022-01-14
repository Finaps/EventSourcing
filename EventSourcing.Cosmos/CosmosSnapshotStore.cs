using EventSourcing.Core;

namespace EventSourcing.Cosmos;

public class CosmosSnapshotStore : CosmosClientBase<SnapshotEvent>, ISnapshotStore
{
    private readonly Container _snapshots;

    public CosmosSnapshotStore(IOptions<CosmosEventStoreOptions> options) : base(options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SnapshotsContainer))
            throw new ArgumentException("CosmosEventStoreOptions.SnapshotContainer should not be empty",
                nameof(options));

        _snapshots = _database.GetContainer(options.Value.SnapshotsContainer);
    }

    /// <summary>
    /// Snapshots: Queryable and AsyncEnumerable Collection of <see cref="Event"/>s
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when snapshot container is not provided</exception>
    public IQueryable<SnapshotEvent> Snapshots => _snapshots.AsCosmosAsyncQueryable<SnapshotEvent>();

    /// <summary>
    /// AddSnapshotAsync: Store snapshot as a <see cref="Event"/>s to the Cosmos Event Store
    /// </summary>
    /// <param name="snapshot"><see cref="Event"/>s to add</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <exception cref="InvalidOperationException">Thrown when snapshot container is not provided</exception>
    /// <exception cref="ArgumentException">Thrown when trying to add <see cref="Event"/>s with empty AggregateId</exception>
    /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="Event"/>s</exception>
    public async Task AddAsync(SnapshotEvent snapshot, CancellationToken cancellationToken = default)
    {
        EventValidation.Validate(snapshot.PartitionId, new List<SnapshotEvent> { snapshot });

        var transaction = _snapshots.CreateTransactionalBatch(new PartitionKey(snapshot.PartitionId.ToString()));
        transaction.CreateItem(snapshot);
        
        var response = await transaction.ExecuteAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) CosmosExceptionHelpers.Throw(response);
    }
}