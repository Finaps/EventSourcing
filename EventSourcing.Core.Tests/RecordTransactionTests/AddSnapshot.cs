using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_AddSnapshot_Can_Add_Snapshot_In_Transaction()
    {
        var e = new SnapshotEvent
            { AggregateId = Guid.NewGuid(), AggregateType = nameof(SnapshotAggregate), Index = 0 };
        var s = new SnapshotSnapshot 
            { AggregateId = e.AggregateId, AggregateType = nameof(SnapshotAggregate), Index = 0, Counter = 10};
        
        await RecordStore.CreateTransaction()
            .AddEvents(new Event[] { e })
            .AddSnapshot(s)
            .CommitAsync();

        var snapshot = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == s.AggregateId)
            .AsAsyncEnumerable()
            .SingleAsync() as SnapshotSnapshot;
        
        Assert.NotNull(snapshot);
        Assert.Equal(s.Counter, snapshot!.Counter);
    }
    
    [Fact]
    public async Task RecordTransaction_AddSnapshot_Cannot_Add_Invalid_Snapshot_In_Transaction()
    {
        var s = new SnapshotSnapshot { AggregateId = Guid.NewGuid(), AggregateType = nameof(SnapshotAggregate) , Index = -1};

        var transaction = RecordStore.CreateTransaction();
        Assert.Throws<RecordValidationException>(() => transaction.AddSnapshot(s));

        var count = await RecordStore
            .GetSnapshots<SnapshotAggregate>()
            .Where(x => x.AggregateId == s.AggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(0, count);
    }
}