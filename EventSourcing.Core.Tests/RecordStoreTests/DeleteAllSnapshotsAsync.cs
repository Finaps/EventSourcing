using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Can_Delete_Snapshots()
    {
        var store = RecordStore;
        
        var aggregate = new SnapshotAggregate();
        var factory = new SimpleSnapshotFactory();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var e = aggregate.Apply(new SnapshotEvent());
            await store.AddEventsAsync(new List<Event> { e });
            var snapshot = factory.CreateSnapshot(aggregate);
            await store.AddSnapshotAsync(snapshot);
        }

        var countBeforeDelete = await store
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(3, countBeforeDelete);

        await store.DeleteAllSnapshotsAsync<SnapshotAggregate>(aggregate.Id);
        
        var countAfterDelete = await store
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, countAfterDelete);
    }
    
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Can_Only_Delete_Snapshots()
    {
        var aggregate = new EmptyAggregate();
        var events = new List<Event>();
        var snapshot = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate)};
        var snapshot2 = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Index = 1};
        var projection = new EmptyProjection { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Hash = "RANDOM"};

        for (var i = 0; i < 3; i++)
            events.Add(aggregate.Apply(new EmptyEvent()));
    
    
        await RecordStore.AddEventsAsync(events);
        await RecordStore.AddSnapshotAsync(snapshot);
        await RecordStore.AddSnapshotAsync(snapshot2);
        await RecordStore.UpsertProjectionAsync(projection);
    
        await RecordStore.DeleteAllSnapshotsAsync<EmptyAggregate>(aggregate.Id);

        var eventCount = await RecordStore
            .GetEvents<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();

        var snapshotCount = await RecordStore
            .GetSnapshots<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
    
        var projectionResult = await RecordStore.GetProjectionByIdAsync<EmptyProjection>(aggregate.Id);
    
        Assert.Equal(3, eventCount);
        Assert.Equal(0, snapshotCount);
        Assert.NotNull(projectionResult);
    }
    
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Correctly_Returns_Deleted_Count()
    {
        var store = RecordStore;
        
        var aggregate = new SnapshotAggregate();
        var factory = new SimpleSnapshotFactory();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var e = aggregate.Apply(new SnapshotEvent());
            await store.AddEventsAsync(new List<Event> { e });
            var snapshot = factory.CreateSnapshot(aggregate);
            await store.AddSnapshotAsync(snapshot);
        }
        
        var deleted = await store.DeleteAllSnapshotsAsync<SnapshotAggregate>(aggregate.Id);
        
        var countAfterDelete = await store
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, countAfterDelete);
        Assert.Equal(3, deleted);
    }
}