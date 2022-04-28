using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public class AggregateTests
{
  [Fact]
  public void Ctor_Sets_Type()
  {
    var aggregate = new SimpleAggregate();
    Assert.Equal(nameof(SimpleAggregate), aggregate.Type);
  }

  [Fact]
  public void Ctor_Sets_Id()
  {
    var aggregate = new SimpleAggregate();
    Assert.NotEqual(Guid.Empty, aggregate.Id);
  }
  
  [Fact]
  public void Ctor_Sets_Version()
  {
    var aggregate = new SimpleAggregate();
    Assert.Equal(0, aggregate.Version);
  }

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

  [Fact]
  public void Apply_Event_Updates_Version()
  {
    var aggregate = new SimpleAggregate();
    Assert.Equal(0, aggregate.Version);
    
    aggregate.Apply(new SimpleEvent());
    Assert.Equal(1, aggregate.Version);
  }
  
  [Fact]
  public void Cannot_Apply_Event_With_Empty_Aggregate_Id()
  {
    var aggregate = new SimpleAggregate { Id = Guid.Empty };
    Assert.Throws<RecordValidationException>(() => aggregate.Apply(new SimpleEvent()));
  }
}