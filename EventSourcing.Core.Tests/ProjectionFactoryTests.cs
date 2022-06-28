namespace Finaps.EventSourcing.Core.Tests;

public class ProjectionFactoryTests
{
  [Fact]
  public void Can_Project_Aggregate()
  {
    var aggregate = new MockAggregate();
    aggregate.Apply(new MockEvent { MockDecimal = 3.33m, MockEnum = MockEnum.B, MockNullableString = "Test" });
    var projection = aggregate.Project<MockAggregateProjection>();

    Assert.NotNull(projection);
    Assert.Equal(aggregate.Id, projection!.AggregateId);
    Assert.Equal(aggregate.Version, projection.Version);
    Assert.Equal(aggregate.MockDecimal, projection.MockDecimal);
    Assert.Equal(aggregate.MockEnum, projection.MockEnum);
    Assert.Equal(aggregate.MockNullableString, projection.MockNullableString);
  }

  [Fact]
  public void Can_Project_Null()
  {
    var aggregate = new EmptyAggregate();
    aggregate.Apply(new EmptyEvent());
    var projection = aggregate.Project<NullProjection>();
    Assert.Null(projection);
  }

  [Fact]
  public void Cannot_Project_With_NonExistent_ProjectionFactory()
  {
    var aggregate = new EmptyAggregate();
    aggregate.Apply(new EmptyEvent());

    // There is no factory projecting from EmptyAggregate to MockAggregateProjection
    Assert.Throws<ArgumentException>(() => aggregate.Project<MockAggregateProjection>());
  }
}