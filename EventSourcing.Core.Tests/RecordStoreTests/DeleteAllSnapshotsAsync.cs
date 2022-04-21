namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Can_Delete_Snapshots()
    {
        var aggregate = new SnapshotAggregate();
        var factory = new SimpleSnapshotFactory();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var e = aggregate.Apply(new SnapshotEvent());
            await RecordStore.AddEventsAsync(new List<Event> { e });
            var snapshot = factory.CreateSnapshot(aggregate);
            await RecordStore.AddSnapshotAsync(snapshot);
        }

        var countBeforeDelete = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(3, countBeforeDelete);

        await RecordStore.DeleteAllSnapshotsAsync<SnapshotAggregate>(aggregate.Id);
        
        var countAfterDelete = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, countAfterDelete);
    }
}