namespace Finaps.EventSourcing.Core.Tests;

public class ProjectionFactoryTests
{
    [Fact]
    public void Can_Project_Aggregate()
    {
        var aggregate = new MockAggregate();
        aggregate.Apply(new MockEvent { MockDecimal = 3.33m, MockEnum = MockEnum.B, MockNullableString = "Test"});
        var projection = aggregate.Project<MockAggregateProjection>();
        
        Assert.NotNull(projection);
        Assert.Equal(aggregate.Id, projection.AggregateId);
        Assert.Equal(aggregate.Version, projection.Version);
        Assert.Equal(aggregate.MockDecimal, projection.MockDecimal);
        Assert.Equal(aggregate.MockEnum, projection.MockEnum);
        Assert.Equal(aggregate.MockNullableString, projection.MockNullableString);
    }
}