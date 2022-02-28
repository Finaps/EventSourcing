using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  [Fact]
  public async Task Can_Delete_Snapshot()
  {
    var snapshot = new EmptySnapshot { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };
    await RecordStore.AddSnapshotAsync(snapshot);

    Assert.NotNull(await RecordStore.Snapshots
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    await RecordStore.DeleteSnapshotAsync(snapshot.AggregateId, snapshot.Index);

    Assert.False(await RecordStore.Snapshots
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .AnyAsync());
  }
}
