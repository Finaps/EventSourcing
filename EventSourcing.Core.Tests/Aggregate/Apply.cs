namespace EventSourcing.Core.Tests;

public class AggregateTests
{
  [Fact]
  public void Aggregate_Apply_Can_Apply_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());

    Assert.Equal(1, aggregate.Counter);
  }

  [Fact]
  public void Aggregate_Apply_Can_Apply_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent())
    };

    Assert.Equal(events.Count, aggregate.Counter);
  }
  
  [Fact]
  public Task Aggregate_Apply_Cannot_Apply_Event_With_Empty_Aggregate_Id()
  {
    var aggregate = new SimpleAggregate { Id = Guid.Empty };
    Assert.Throws<RecordValidationException>(() => aggregate.Apply(new SimpleEvent()));
    
    return Task.CompletedTask;
  }
}