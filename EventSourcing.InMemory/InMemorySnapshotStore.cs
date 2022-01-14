using System.Collections.Concurrent;
using EventSourcing.Core;

namespace EventSourcing.InMemory;

public class InMemorySnapshotStore : ISnapshotStore
{
  private readonly ConcurrentDictionary<(Guid, Guid, long), SnapshotEvent> _storedSnapshots = new();
  public IQueryable<SnapshotEvent> Snapshots => new MockAsyncQueryable<SnapshotEvent>(_storedSnapshots.Values.AsQueryable());

  public Task AddAsync(SnapshotEvent snapshot, CancellationToken cancellationToken = default)
  {
    if (snapshot == null)
      throw new ArgumentNullException(nameof(snapshot));

    if (_storedSnapshots.ContainsKey((snapshot.PartitionId, snapshot.AggregateId, snapshot.AggregateVersion)))
      throw new EventStoreException(snapshot);

    if (!_storedSnapshots.TryAdd((snapshot.PartitionId, snapshot.AggregateId, snapshot.AggregateVersion), snapshot))
      throw new EventStoreException(snapshot);

    return Task.CompletedTask;
  }
}