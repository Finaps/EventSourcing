namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task RecordTransaction_DeleteAllEvents_Can_Delete_All_Events_In_Transaction()
    {
        var aggregateId = Guid.NewGuid();
        
        var events = Enumerable.Range(0, 5)
            .Select<int, Event>( i => new EmptyEvent
            {
                AggregateId = aggregateId,
                AggregateType = nameof(EmptyAggregate),
                Index = i
            }).ToList();

        await RecordStore.CreateTransaction()
          .AddEvents(events)
          .CommitAsync();
        
        await RecordStore.CreateTransaction()
            .DeleteAllEvents<EmptyAggregate>(aggregateId, events.Count - 1)
            .CommitAsync();

        var count = await RecordStore
            .GetEvents<EmptyAggregate>()
            .Where(x => x.AggregateId == aggregateId)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(0, count);
    }
}