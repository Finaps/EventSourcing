using System.Reflection;
using EventSourcing.Core.Records;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
    [Fact]
    public async Task Can_Store_Attribute_Event_With_Correct_Type()
    {
        var e = new EmptyAggregate().Add(new AttributeEvent("something"));
        var recordType = e.GetType().GetCustomAttribute<RecordTypeAttribute>()!.Value;

        await EventStore.AddAsync(new List<Event>{e});
        var result = await EventStore.Events
            .Where(r => r.Id == e.Id && r.Type == recordType)
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync() as AttributeEvent;

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Can_Deserialize_Attribute_Event_With_Custom_Type_Name()
    {
        var e = new EmptyAggregate().Add(new AttributeEvent("something"));

        await EventStore.AddAsync(new List<Event>{e});

        var result = await EventStore.Events
            .Where(r => r.Id == e.Id)
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync() as AttributeEvent;

        Assert.NotNull(result);
        Assert.Equal(typeof(AttributeEvent).GetCustomAttribute<RecordTypeAttribute>()!.Value, result!.Type);
        Assert.Equal(e.SomeString, result.SomeString);
    }
}