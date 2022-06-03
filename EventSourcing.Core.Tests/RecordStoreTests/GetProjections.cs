namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_GetProjections_Can_Query_Aggregate_Projection_After_Persisting()
  {
    var aggregate = MockAggregate.Create();

    await AggregateService.PersistAsync(aggregate);

    var projection = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    IMock.AssertEqual(aggregate, projection);
  }
}