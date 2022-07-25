namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Can_Delete_Snapshots()
    {
        var store = GetRecordStore();
        
        var aggregate = new SnapshotAggregate();
        var factory = new SimpleSnapshotFactory();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var e = aggregate.Apply(new SnapshotEvent());
            await store.AddEventsAsync(new [] { e });
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
        var snapshot = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate)};
        var snapshot2 = new EmptySnapshot { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Index = 1};
        var projection = new EmptyProjection { AggregateId = aggregate.Id, AggregateType = nameof(EmptyAggregate), Hash = "RANDOM"};

        var events = Enumerable
            .Range(0, 3)
            .Select(_ => aggregate.Apply(new EmptyEvent()))
            .ToArray();

        await GetRecordStore().AddEventsAsync(events);
        await GetRecordStore().AddSnapshotAsync(snapshot);
        await GetRecordStore().AddSnapshotAsync(snapshot2);
        await GetRecordStore().UpsertProjectionAsync(projection);
    
        await GetRecordStore().DeleteAllSnapshotsAsync<EmptyAggregate>(aggregate.Id);

        var eventCount = await GetRecordStore()
            .GetEvents<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();

        var snapshotCount = await GetRecordStore()
            .GetSnapshots<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
    
        var projectionResult = await GetRecordStore().GetProjectionByIdAsync<EmptyProjection>(aggregate.Id);
    
        Assert.Equal(3, eventCount);
        Assert.Equal(0, snapshotCount);
        Assert.NotNull(projectionResult);
    }
    
    [Fact]
    public async Task RecordStore_DeleteAllSnapshotsAsync_Correctly_Returns_Deleted_Count()
    {
        var store = GetRecordStore();
        
        var aggregate = new SnapshotAggregate();
        var factory = new SimpleSnapshotFactory();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var e = aggregate.Apply(new SnapshotEvent());
            await store.AddEventsAsync(new [] { e });
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