using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public class AggregateTests
{
  [Fact]
  public void Can_Add_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new EmptyEvent());

    Assert.Single(aggregate.UncommittedEvents);
  }

  [Fact]
  public void Can_Add_Multiple_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    Assert.Equal(events.Count, aggregate.UncommittedEvents.Length);
  }

  [Fact]
  public void Can_Apply_Event()
  {
    var aggregate = new SimpleAggregate();
    aggregate.Apply(new EmptyEvent());

    Assert.Equal(1, aggregate.Counter);
  }

  [Fact]
  public void Can_Apply_Events()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    Assert.Equal(events.Count, aggregate.Counter);
  }
}