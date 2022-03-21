namespace EventSourcing.Core.Tests;

public class AggregateTests
{
  [Fact]
  public void Can_Apply_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new SimpleEvent());

    Assert.Equal(1, aggregate.Counter);
  }

  [Fact]
  public void Can_Apply_Events()
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
}