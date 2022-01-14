using EventSourcing.Core;

namespace EventSourcing.Cosmos;

/// <summary>
/// Cosmos Event Store: Cosmos Connection for Querying and Storing <see cref="Event"/>s
/// </summary>
public class CosmosEventStore : CosmosClientBase<Event>, IEventStore
{
  private readonly Container _container;
  
  /// <summary>
  /// Initialize Cosmos Event Store
  /// </summary>
  /// <param name="options">Cosmos Event Store Options</param>
  /// <exception cref="ArgumentException"></exception>
  public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options)
  {
    if (string.IsNullOrWhiteSpace(options.Value.EventsContainer))
      throw new ArgumentException("CosmosEventStoreOptions.EventsContainer should not be empty", nameof(options));

    _container = _database.GetContainer(options.Value.EventsContainer);
  }
  
  public IQueryable<Event> Events => _container.AsCosmosAsyncQueryable<Event>();
  
  public async Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
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

  public IEventTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction CreateTransaction(Guid partitionId) => 
    new CosmosEventTransaction(_container, partitionId);

}