namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_AddSnapshot_Can_Add_Snapshot_In_Transaction()
    {
        var e = new SnapshotEvent { AggregateId = Guid.NewGuid() };
        var s = new SnapshotSnapshot { AggregateId = e.AggregateId };
        
        await RecordStore.CreateTransaction()
            .AddEvents(new List<Event> { e })
            .AddSnapshot(s)
            .CommitAsync();

        var count = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == s.AggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(1, count);
    }
    
    [Fact]
    public async Task RecordTransaction_AddSnapshot_Cannot_Add_Invalid_Snapshot_In_Transaction()
    {
        var s = new Snapshot { AggregateType = nameof(EmptyAggregate) };
        
        await Assert.ThrowsAsync<RecordValidationException>(async () => await RecordStore.CreateTransaction()
            .AddSnapshot(s)
            .CommitAsync());

        var count = await RecordStore
            .GetSnapshots<EmptyAggregate>()
            .Where(x => x.AggregateId == s.AggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(0, count);
    }
}