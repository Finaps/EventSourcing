using System.Collections.Concurrent;
using EventSourcing.Core;

namespace EventSourcing.InMemory;

public class InMemorySnapshotStore : ISnapshotStore
{
  private readonly ConcurrentDictionary<(Guid, Guid, long), Snapshot> _storedSnapshots = new();
  public IQueryable<Snapshot> Snapshots => new MockAsyncQueryable<Snapshot>(_storedSnapshots.Values.AsQueryable());

  public Task AddAsync(Snapshot snapshot, CancellationToken cancellationToken = default)
  {
    if (snapshot == null)
      throw new ArgumentNullException(nameof(snapshot));

    if (_storedSnapshots.ContainsKey((snapshot.PartitionId, snapshot.AggregateId, snapshot.Index)))
      throw new EventStoreException(snapshot);

    if (!_storedSnapshots.TryAdd((snapshot.PartitionId, snapshot.AggregateId, snapshot.Index), snapshot))
      throw new EventStoreException(snapshot);

    return Task.CompletedTask;
  }
}