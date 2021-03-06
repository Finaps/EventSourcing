namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_GetProjections_Can_Query_Aggregate_Projections_After_Persisting()
  {
    var aggregate = MockAggregate.Create();

    await GetAggregateService().PersistAsync(aggregate);

    var projection = await GetRecordStore()
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    IMock.AssertEqual(aggregate, projection);
  }
  
  [Fact]
  public async Task RecordStore_GetProjections_Can_Query_Aggregate_Projection_After_Persisting()
  {
    var aggregate = MockAggregate.Create();

    await GetAggregateService().PersistAsync(aggregate);

    var projection = await GetRecordStore().GetProjectionByIdAsync<MockAggregateProjection>(aggregate.Id);
    
    IMock.AssertEqual(aggregate, projection!);
  }
  
  [Fact]
  public async Task RecordStore_GetProjections_Can_Query_Aggregate_Projection_Hierarchy_After_Persisting()
  {
    var partitionId = Guid.NewGuid();

    for (var i = 0; i < 3; i++)
    {
      var aggregate = new HierarchyAggregate { PartitionId = partitionId };
      aggregate.Apply(i switch
        {
          0 => new HierarchyEvent("AA", "B", "C"),
          1 => new HierarchyEvent("A", "BB", "C"),
          _ => new HierarchyEvent("A", "B", "CC")
        }
      );
      await GetAggregateService().PersistAsync(aggregate);
    }
    
    var projections = await GetRecordStore().GetProjections<HierarchyProjection>()
      .Where(x => x.PartitionId == partitionId)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.Equal(3, projections.Count);
    Assert.Single(projections, x => x is HierarchyProjectionA);
    Assert.Single(projections, x => x is HierarchyProjectionB);
    Assert.Single(projections, x => x is HierarchyProjectionC);
  }
}