namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_AddSnapshot_Can_Add_Snapshot_In_Transaction()
    {
        var s = new Snapshot { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };
        
        await RecordStore.CreateTransaction()
            .AddSnapshot(s)
            .CommitAsync();

        var count = await RecordStore
            .GetSnapshots<EmptyAggregate>()
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