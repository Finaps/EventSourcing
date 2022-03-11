using System.Reflection;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
    [Fact]
    public async Task Can_Store_Attribute_Event_With_Correct_Type()
    {
        var e = new EmptyAggregate().Apply(new AttributeEvent("something"));
        var recordType = e.GetType().GetCustomAttribute<RecordTypeAttribute>()!.Type;

        await RecordStore.AddEventsAsync(new List<Event>{e});
        var result = await RecordStore.Events
            .Where(x => x.Type == recordType && x.AggregateId == e.AggregateId)
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync() as AttributeEvent;

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Can_Deserialize_Attribute_Event_With_Custom_Type_Name()
    {
        var e = new EmptyAggregate().Apply(new AttributeEvent("something"));

        await RecordStore.AddEventsAsync(new List<Event>{e});

        var result = await RecordStore.Events
            .Where(x => x.Type == e.Type && x.AggregateId == e.AggregateId)
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync() as AttributeEvent;

        Assert.NotNull(result);
        Assert.Equal(typeof(AttributeEvent).GetCustomAttribute<RecordTypeAttribute>()!.Type, result!.Type);
        Assert.Equal(e.SomeString, result.SomeString);
    }
}