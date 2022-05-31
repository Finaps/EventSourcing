namespace Finaps.EventSourcing.Core.Tests;

public class ProjectionFactoryTests
{
    [Fact]
    public void Can_Project_Aggregate()
    {
        var aggregate = new EmptyAggregate();
        aggregate.Apply(new EmptyEvent());
        var projection = aggregate.Project<EmptyProjection>();
        
        Assert.NotNull(projection);
        Assert.Equal(aggregate.Id, projection.AggregateId);
        Assert.Equal(aggregate.Version, projection.Version);
    }
}