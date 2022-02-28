using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  [Fact]
  public async Task Can_Delete_Projection()
  {
    var projection = new EmptyProjection { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };
    await RecordStore.AddProjectionAsync(projection);

    Assert.NotNull(await RecordStore.GetProjectionByIdAsync<EmptyProjection>(projection.AggregateId));

    await RecordStore.DeleteProjectionAsync<EmptyProjection>(projection.AggregateId);

    await Assert.ThrowsAsync<RecordStoreException>(async () =>
      await RecordStore.GetProjectionByIdAsync<EmptyProjection>(projection.AggregateId));
  }
}