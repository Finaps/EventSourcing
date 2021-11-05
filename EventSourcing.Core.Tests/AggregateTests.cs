using System.Collections.Generic;
using EventSourcing.Core.Tests.MockAggregates;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public class AggregateTests
  {
    [Fact]
    public void Can_Add_Event()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());

      Assert.Single(aggregate.UncommittedEvents);
    }

    [Fact]
    public void Can_Add_Multiple_Events()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      Assert.Equal(events.Count, aggregate.UncommittedEvents.Length);
    }

    [Fact]
    public void Can_Clear_Uncommitted_Events()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());
      aggregate.Add(new EmptyEvent());

      aggregate.ClearUncommittedEvents();

      Assert.Empty(aggregate.UncommittedEvents);
    }

    [Fact]
    public void Can_Apply_Event()
    {
      var aggregate = new SimpleAggregate();
      aggregate.Add(new EmptyEvent());

      Assert.Equal(1, aggregate.Counter);
    }

    [Fact]
    public void Can_Apply_Events()
    {
      var aggregate = new SimpleAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      Assert.Equal(events.Count, aggregate.Counter);
    }
  }
}