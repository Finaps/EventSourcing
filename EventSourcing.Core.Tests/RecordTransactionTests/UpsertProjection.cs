using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_UpsertProjection_Can_Upsert_Projection_In_Transaction()
    {
        var e = new EmptyEvent();
        var a = new EmptyAggregate();
        a.Apply(e);
        await AggregateService.PersistAsync(a);

        var projectionBefore = await RecordStore
            .GetProjections<EmptyProjection>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .SingleAsync();
        
        var updatedProjection = projectionBefore with { Timestamp = DateTimeOffset.Now };
        
        Assert.NotEqual(projectionBefore.Timestamp, updatedProjection.Timestamp);
        
        await RecordStore.CreateTransaction()
            .UpsertProjection(updatedProjection)
            .CommitAsync();

        var result = await RecordStore
            .GetProjections<EmptyProjection>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .SingleAsync();
        
        Assert.Equal(updatedProjection.Timestamp.DateTime, result.Timestamp.DateTime,
            
            // Postgres has 1 ms precision, so accommodate
            TimeSpan.FromMilliseconds(1));
    }
}