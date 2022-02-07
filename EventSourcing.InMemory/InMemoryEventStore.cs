using System.Collections.Concurrent;
using EventSourcing.Core;

namespace EventSourcing.InMemory;

public class InMemoryEventStore : IEventStore
{
  private readonly ConcurrentDictionary<(Guid, Guid, long), Event> _events = new();
    
  public IQueryable<Event> Events => new MockAsyncQueryable<Event>(_events.Values.AsQueryable());

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

  public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public Task<long> GetAggregateVersionAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var index = _events.Values
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderByDescending(i => i)
      .FirstOrDefault();
    
    if (index == 0)
      throw new EventStoreException($"Cannot get version of nonexistent Aggregate with PartitionId '{partitionId}' and Id '{aggregateId}'");
    
    return Task.FromResult(1 + index);
  }

  public async Task<long> GetAggregateVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await GetAggregateVersionAsync(Guid.Empty, aggregateId, cancellationToken);

  public IEventTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction CreateTransaction(Guid partitionId) =>
    new InMemoryEventTransaction(_events, partitionId);
}