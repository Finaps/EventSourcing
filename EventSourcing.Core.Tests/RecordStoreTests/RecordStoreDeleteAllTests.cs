using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
    [Fact]
    public async Task Can_Delete_Aggregate_All()
    {
        var aggregate = new EmptyAggregate();
        var events = new List<Event>();

        for (var i = 0; i < 3; i++)
            events.Add(aggregate.Add(new EmptyEvent()));

        await RecordStore.AddEventsAsync(events);
        
        var deleted = await RecordStore.DeleteAggregateAllAsync(Guid.Empty, aggregate.Id);
        
        var count = await RecordStore.Events
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(3, deleted);
        Assert.Equal(0, count);
    }
    
    [Fact]
    public async Task Can_Delete_More_Than_100_Events_Using_DeleteAggregateAll()
    {
        var aggregate = new EmptyAggregate();
        var events = new List<Event>();

        for (var i = 0; i < 100; i++)
            events.Add(aggregate.Add(new EmptyEvent()));

        await RecordStore.AddEventsAsync(events);
        events.Clear();
        
        for (var i = 0; i < 10; i++)
            events.Add(aggregate.Add(new EmptyEvent()));

        await RecordStore.AddEventsAsync(events);
        var deleted = await RecordStore.DeleteAggregateAllAsync(Guid.Empty, aggregate.Id);
        
        var count = await RecordStore.Events
            .Where(x => x.AggregateId == aggregate.Id)
            .AsAsyncEnumerable()
            .CountAsync();
        
        Assert.Equal(110, deleted);
        Assert.Equal(0, count);
    }
}