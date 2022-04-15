namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_DeleteSnapshot_Can_Delete_Snapshot_In_Transaction()
    {
        var a = new SnapshotAggregate();
        var events = Enumerable.Range(0, 10).Select(i => new SnapshotEvent
        {
            Index = i,
            AggregateId = a.Id
        }).ToList();
        
        foreach (var snapshotEvent in events)
            a.Apply(snapshotEvent);
        
        await AggregateService.PersistAsync(a);
        
        await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .SingleAsync();
        
        await RecordStore.CreateTransaction()
            .DeleteSnapshot<SnapshotAggregate>(a.Id, events.Count - 1)
            .CommitAsync();

        var countAfter = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(0, countAfter);
    }
}