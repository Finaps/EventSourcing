namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_DeleteProjection_Can_Delete_Projection_In_Transaction()
    {
        var e = new EmptyEvent();
        var a = new EmptyAggregate();
        a.Apply(e);
        await GetAggregateService().PersistAsync(a);
        
        var countBefore = await GetRecordStore()
            .GetProjections<EmptyProjection>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(1, countBefore);
        
        await GetRecordStore().CreateTransaction()
            .DeleteProjection<EmptyProjection>(a.Id)
            .CommitAsync();

        var countAfter = await GetRecordStore()
            .GetProjections<EmptyProjection>()
            .Where(x => x.AggregateId == a.Id)
            .AsAsyncEnumerable()
            .CountAsync();
    
        Assert.Equal(0, countAfter);
    }
}