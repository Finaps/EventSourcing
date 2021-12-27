using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core.Tests.MockDatabase
{
    internal class InMemorySnapshotStore : InMemorySnapshotStore<Event>, ISnapshotStore
    {
    }
    internal class InMemorySnapshotStore<TBaseEvent> : ISnapshotStore<TBaseEvent>
        where TBaseEvent: Event, new()
    {
        private readonly ConcurrentDictionary<(Guid, uint), TBaseEvent> _storedSnapshots = new();
        public IQueryable<TBaseEvent> Snapshots => new MockAsyncQueryable<TBaseEvent>(_storedSnapshots.Values.AsQueryable());
        
        public Task AddSnapshotAsync(TBaseEvent snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (_storedSnapshots.ContainsKey((snapshot.AggregateId, snapshot.AggregateVersion)))
                throw new ConcurrencyException(snapshot);

            if (!_storedSnapshots.TryAdd((snapshot.AggregateId, snapshot.AggregateVersion), snapshot))
                throw new ConcurrencyException(snapshot);

            return Task.CompletedTask;
        }
    }
}