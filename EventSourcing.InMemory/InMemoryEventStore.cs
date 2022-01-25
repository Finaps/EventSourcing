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
      .Delete(aggregateId, await GetVersionAsync(partitionId, aggregateId, cancellationToken))
      .CommitAsync(cancellationToken);
  }

  public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public Task<long> GetVersionAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(_events.Values
      .Where(x => x.PartitionId == partitionId && x.AggregateId == aggregateId)
      .Select(x => x.Index)
      .OrderByDescending(i => i)
      .First());
  }

  public async Task<long> GetVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    await GetVersionAsync(Guid.Empty, aggregateId, cancellationToken);

  public IEventTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction CreateTransaction(Guid partitionId) =>
    new InMemoryEventTransaction(_events, partitionId);
}