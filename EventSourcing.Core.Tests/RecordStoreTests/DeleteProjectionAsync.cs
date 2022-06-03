namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteProjectionAsync_Can_Delete_Projection()
  {
    var projection = new EmptyProjection { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate), Hash = "RANDOM" };
    await RecordStore.UpsertProjectionAsync(projection);

    Assert.NotNull(await RecordStore.GetProjectionByIdAsync<EmptyProjection>(projection.AggregateId));

    await RecordStore.DeleteProjectionAsync<EmptyProjection>(projection.AggregateId);
    
    Assert.Null(await RecordStore.GetProjectionByIdAsync<EmptyProjection>(projection.AggregateId));
  }
}