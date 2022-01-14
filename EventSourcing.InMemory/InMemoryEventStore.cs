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

  public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default) =>
    DeleteAsync(Guid.Empty, aggregateId, cancellationToken);

  public IEventTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction CreateTransaction(Guid partitionId) =>
    new InMemoryEventTransaction(_events, partitionId);
}