using System.Reflection;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract class RecordAttributeTests
{
    protected abstract IEventStore EventStore { get; }
    
    [Fact]
    public async Task Can_Store_Attribute_Event_With_Correct_Type()
    {
        var e = new EmptyAggregate().Add(new AttributeEvent("something"));
        
        Assert.Equal(typeof(AttributeEvent).GetCustomAttribute<RecordName>()?.Value, e.Type);
        
        await EventStore.AddAsync(new List<Event>{e});
        var result = EventStore.Events.FirstOrDefault(r => r.RecordId == e.RecordId && r.Type == e.Type) as AttributeEvent;
        
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task Can_Deserialize_Attribute_Event_With_Custom_Type_Name()
    {
        var e = new EmptyAggregate().Add(new AttributeEvent("something"));
        await EventStore.AddAsync(new List<Event>{e});
        
        var result = EventStore.Events.FirstOrDefault(r => r.RecordId == e.RecordId) as AttributeEvent;
        
        Assert.NotNull(result);
        Assert.Equal(typeof(AttributeEvent).GetCustomAttribute<RecordName>()?.Value, result.Type);
        Assert.Equal(e.SomeString, result.SomeString);
    }
}