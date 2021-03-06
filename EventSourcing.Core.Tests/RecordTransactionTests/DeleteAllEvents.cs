namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_DeleteAllEvents_Can_Delete_All_Events_In_Transaction()
    {
        var aggregateId = Guid.NewGuid();
        
        var events = Enumerable.Range(0, 5)
            .Select<int, Event<EmptyAggregate>>(i => new EmptyEvent
            {
                AggregateId = aggregateId,
                AggregateType = nameof(EmptyAggregate),
                Index = i
            }).ToList();

        await GetRecordStore().CreateTransaction()
          .AddEvents(events)
          .CommitAsync();
        
        await GetRecordStore().CreateTransaction()
            .DeleteAllEvents<EmptyAggregate>(aggregateId, events.Count - 1)
            .CommitAsync();

        var count = await GetRecordStore()
            .GetEvents<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, count);
    }
    
    [Fact]
    public async Task RecordTransaction_DeleteAllEvents_Cannot_Can_Delete_And_Add_For_Same_Aggregate_In_One_Transaction()
    {
        var aggregateId = Guid.NewGuid();
        
        var events = Enumerable.Range(0, 5)
            .Select<int, Event>( i => new EmptyEvent
            {
                AggregateId = aggregateId,
                AggregateType = nameof(EmptyAggregate),
                Index = i
            }).ToList();

        var transaction = GetRecordStore().CreateTransaction()
            .AddEvents(new List<Event<EmptyAggregate>>
            {
                new EmptyEvent
                {
                    AggregateId = aggregateId,
                    AggregateType = nameof(EmptyAggregate),
                    Index = events.Count
                }
            })
            .DeleteAllEvents<EmptyAggregate>(aggregateId, events.Count);

        await Assert.ThrowsAsync<RecordStoreException>(async () => await transaction.CommitAsync());
        
        var count = await GetRecordStore()
            .GetEvents<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, count);
    }
}