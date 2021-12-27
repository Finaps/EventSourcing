using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;
using EventSourcing.Core.Tests.MockAggregates;
using Xunit;

namespace EventSourcing.Core.Tests
{
    public abstract class SnapshotStoreTests
    {
        protected abstract ISnapshotStore GetSnapshotStore();
        protected abstract ISnapshotStore<TBaseEvent> GetSnapshotStore<TBaseEvent>() where TBaseEvent : Event, new();
        [Fact]
        public async Task Can_Add_Snapshot()
        {
            var store = GetSnapshotStore();
            var aggregate = new SnapshotAggregate();
            aggregate.Add(aggregate.CreateSnapshot() as SnapshotEvent);
            var snapshot = aggregate.UncommittedEvents.First();
            await store.AddSnapshotAsync(snapshot);
        }
        
        [Fact]
        public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Version()
        {
            var store = GetSnapshotStore();

            var aggregate = new SnapshotAggregate();
            aggregate.Add(aggregate.CreateSnapshot());
            var snapshot = aggregate.UncommittedEvents.First();
      
            await store.AddSnapshotAsync(snapshot);

            var exception = await Assert.ThrowsAnyAsync<ConcurrencyException>(
                async () => await store.AddSnapshotAsync(snapshot));
        }
        
        [Fact]
        public async Task Can_Get_Snapshot_By_AggregateId()
        {
            var store = GetSnapshotStore();

            var aggregate = new SnapshotAggregate();
            aggregate.Add(aggregate.CreateSnapshot());
            await store.AddSnapshotAsync(aggregate.UncommittedEvents.First());

            var result = await store.Snapshots
                .Where(x => x.AggregateId == aggregate.Id)
                .ToListAsync();

            Assert.Single(result);
        }
        
        [Fact]
        public async Task Can_Get_Latest_Snapshot_By_AggregateId()
        {
            var store = GetSnapshotStore();

            var aggregate = new SnapshotAggregate();
            aggregate.Add(aggregate.CreateSnapshot());
            aggregate.Add(new EmptyEvent());
            aggregate.Add(aggregate.CreateSnapshot());

            var snapshot = aggregate.UncommittedEvents[0];
            var snapshot2 = aggregate.UncommittedEvents[2];
            Assert.NotEqual(snapshot.AggregateVersion, snapshot2.AggregateVersion);
      
            await store.AddSnapshotAsync(snapshot);
            await store.AddSnapshotAsync(snapshot2);

            var result = (await store.Snapshots
                .Where(x => x.AggregateId == aggregate.Id)
                .OrderByDescending(x => x.AggregateVersion)
                .ToListAsync()).FirstOrDefault();
      
            Assert.NotNull(result);
            Assert.Equal(snapshot2.AggregateVersion,result.AggregateVersion);
        }
    }
}