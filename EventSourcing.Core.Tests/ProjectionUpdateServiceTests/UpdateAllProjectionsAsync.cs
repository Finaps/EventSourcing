namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task ProjectionService_UpdateAllProjectionsAsync_Can_Update_Projection_By_Aggregate_And_Projection()
  {
    var aggregate = MockAggregate.Create();

    await GetAggregateService().PersistAsync(aggregate);

    var defaultProjection = new MockAggregateProjection
    {
      AggregateType = aggregate.Type,
      PartitionId = Guid.Empty,
      AggregateId = aggregate.Id,
      
      FactoryType = nameof(MockAggregateProjectionFactory),
      Hash = "OUTDATED",

      MockString = "",
      MockNestedRecord = new MockNestedRecord(default, "", default, default),
      MockNestedRecordList = new List<MockNestedRecordItem>(),
      MockFloatList = new List<float>(),
      MockStringSet = new List<string>()
    };

    await GetRecordStore().UpsertProjectionAsync(defaultProjection);
    
    var before = await GetRecordStore()
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    IMock.AssertDefault(before);

    await GetProjectionUpdateService().UpdateAllProjectionsAsync<MockAggregate, MockAggregateProjection>();

    var after = await GetRecordStore()
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
  
    IMock.AssertEqual(aggregate, after);
  }
}