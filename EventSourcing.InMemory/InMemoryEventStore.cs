using System.Collections.Concurrent;
using EventSourcing.Core;

namespace EventSourcing.InMemory;

public class InMemoryEventStore : InMemoryEventStore<Event>, IEventStore { }

public class InMemoryEventStore<TBaseEvent> : IEventStore<TBaseEvent> where TBaseEvent : Event, new()
{
  private readonly ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> _events = new();
    
  public IQueryable<TBaseEvent> Events => new MockAsyncQueryable<TBaseEvent>(_events.Values.AsQueryable());

  public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;
    
    var transaction = CreateTransaction(events.First().PartitionId);
    await transaction.AddAsync(events, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
  }

  public IEventTransaction<TBaseEvent> CreateTransaction() => CreateTransaction(Guid.Empty);
  public IEventTransaction<TBaseEvent> CreateTransaction(Guid partitionId) =>
    new InMemoryEventTransaction<TBaseEvent>(_events, partitionId);
}