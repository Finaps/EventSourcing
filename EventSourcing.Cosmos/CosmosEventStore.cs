using EventSourcing.Core;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Cosmos;

public class CosmosEventStore : CosmosEventStore<Event>, IEventStore
{
  public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options) { }
}

/// <summary>
/// Cosmos Event Store: Cosmos Connection for Querying and Storing <see cref="TBaseEvent"/>s
/// </summary>
/// <typeparam name="TBaseEvent"></typeparam>
public class CosmosEventStore<TBaseEvent> : CosmosClientBase<TBaseEvent>, IEventStore<TBaseEvent>
  where TBaseEvent : Event, new()
{
  private readonly Container _container;

  public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options)
  {
    if (string.IsNullOrWhiteSpace(options.Value.EventsContainer))
      throw new ArgumentException("CosmosEventStoreOptions.EventsContainer should not be empty", nameof(options));

    _container = _database.GetContainer(options.Value.EventsContainer);
  }

  /// <summary>
  /// Events: Queryable and AsyncEnumerable Collection of <see cref="TBaseEvent"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
  public IQueryable<TBaseEvent> Events => _container.AsCosmosAsyncQueryable<TBaseEvent>();

  /// <summary>
  /// AddAsync: Store <see cref="TBaseEvent"/>s to the Cosmos Event Store
  /// </summary>
  /// <param name="events"><see cref="TBaseEvent"/>s to add</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="TBaseEvent"/>s with empty AggregateId</exception>
  /// <exception cref="ArgumentException">Thrown when trying to add <see cref="TBaseEvent"/>s with equal AggregateVersions</exception>
  /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="TBaseEvent"/>s</exception>
  /// <exception cref="ConcurrencyException">Thrown when storing <see cref="TBaseEvent"/>s</exception> with existing partition key and version combination
  public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;
    
    var transaction = CreateTransaction(events.First().PartitionId);
    await transaction.AddAsync(events, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var transaction = CreateTransaction(partitionId);
    await transaction.DeleteAsync(aggregateId, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public IEventTransaction<TBaseEvent> CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction<TBaseEvent> CreateTransaction(Guid partitionId) => 
    new CosmosEventTransaction<TBaseEvent>(_container, partitionId);

}