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

    _container = Database.GetContainer(options.Value.EventsContainer);
  }
  
  public IQueryable<Event> Events => _container.AsCosmosAsyncQueryable<Event>();
  
  public async Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;
    
    await CreateTransaction(events.First().PartitionId)
      .Add(events)
      .CommitAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    await CreateTransaction(partitionId)
      .Delete(aggregateId, await GetAggregateVersionAsync(partitionId, aggregateId, cancellationToken))
      .CommitAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public async Task<long> GetAggregateVersionAsync(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default)
  {
    var index = await _container
      .AsCosmosAsyncQueryable<Event>()
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderByDescending(version => version)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync(cancellationToken);

    if (index == 0)
      throw new EventStoreException($"Cannot get version of nonexistent Aggregate with PartitionId '{partitionId}' and Id '{aggregateId}'");

    return index + 1;
  }


  public async Task<long> GetAggregateVersionAsync(Guid aggregateId, CancellationToken cancellationToken) =>
    await GetAggregateVersionAsync(Guid.Empty, aggregateId, cancellationToken);

  public IEventTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction CreateTransaction(Guid partitionId) => 
    new CosmosEventTransaction(_container, partitionId);

}