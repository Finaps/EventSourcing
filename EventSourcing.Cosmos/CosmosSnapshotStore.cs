using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
    public class CosmosSnapshotStore : CosmosSnapshotStore<Event>, ISnapshotStore
    {
        public CosmosSnapshotStore(IOptions<CosmosEventStoreOptions> options) : base(options) { }
    }

    public class CosmosSnapshotStore<TBaseEvent> : CosmosClientBase<TBaseEvent>, ISnapshotStore<TBaseEvent> where TBaseEvent : Event, new()
    {
        private readonly Container _snapshots;

        public CosmosSnapshotStore(IOptions<CosmosEventStoreOptions> options) : base(options)
        {
            if (string.IsNullOrWhiteSpace(options.Value.SnapshotsContainer))
                throw new ArgumentException("CosmosEventStoreOptions.SnapshotContainer should not be empty",
                    nameof(options));

            _snapshots = _database.GetContainer(options.Value.SnapshotsContainer);
        }

        /// <summary>
        /// Snapshots: Queryable and AsyncEnumerable Collection of <see cref="TBaseEvent"/>s
        /// </summary>
        /// <typeparam name="TBaseEvent"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when snapshot container is not provided</exception>
        public IQueryable<TBaseEvent> Snapshots =>
            new CosmosAsyncQueryable<TBaseEvent>(_snapshots.GetItemLinqQueryable<TBaseEvent>());
        
        /// <summary>
        /// AddSnapshotAsync: Store snapshot as a <see cref="TBaseEvent"/>s to the Cosmos Event Store
        /// </summary>
        /// <param name="snapshot"><see cref="TBaseEvent"/>s to add</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <exception cref="InvalidOperationException">Thrown when snapshot container is not provided</exception>
        /// <exception cref="ArgumentException">Thrown when trying to add <see cref="TBaseEvent"/>s with empty AggregateId</exception>
        /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="TBaseEvent"/>s</exception>
        /// <exception cref="ConcurrencyException">Thrown when storing <see cref="TBaseEvent"/>s</exception> with existing partition key and version combination
        public async Task AddSnapshotAsync(TBaseEvent snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.AggregateId == Guid.Empty)
                throw new ArgumentException(
                    "AggregateId should be set", nameof(snapshot));
            try
            {
                await _snapshots.CreateItemAsync(snapshot,
                    new PartitionKey(snapshot.AggregateId.ToString()), null, cancellationToken);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                    throw new ConcurrencyException(snapshot, e);
                
                throw new EventStoreException(
                    $"Encountered error while adding events: {(int)e.StatusCode} {e.StatusCode.ToString()}", e);
            }
        }
    }
}