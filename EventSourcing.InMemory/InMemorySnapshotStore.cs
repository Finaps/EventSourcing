using System.Collections.Concurrent;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.InMemory;

public class InMemorySnapshotStore : InMemorySnapshotStore<Event>, ISnapshotStore { }

public class InMemorySnapshotStore<TBaseEvent> : ISnapshotStore<TBaseEvent>
  where TBaseEvent : Event, new()
{
  private readonly ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> _storedSnapshots = new();
  public IQueryable<TBaseEvent> Snapshots => new MockAsyncQueryable<TBaseEvent>(_storedSnapshots.Values.AsQueryable());

  public Task AddSnapshotAsync(TBaseEvent snapshot, CancellationToken cancellationToken = default)
  {
    if (snapshot == null)
      throw new ArgumentNullException(nameof(snapshot));

    if (_storedSnapshots.ContainsKey((snapshot.PartitionId, snapshot.AggregateId, snapshot.AggregateVersion)))
      throw new ConcurrencyException(snapshot);

    if (!_storedSnapshots.TryAdd((snapshot.PartitionId, snapshot.AggregateId, snapshot.AggregateVersion), snapshot))
      throw new ConcurrencyException(snapshot);

    return Task.CompletedTask;
  }
}