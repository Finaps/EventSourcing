namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task Can_Delete_Snapshot()
  {
    var snapshot = new EmptySnapshot { AggregateId = Guid.NewGuid(), AggregateType = nameof(EmptyAggregate) };
    await RecordStore.AddSnapshotAsync(snapshot);

    Assert.NotNull(await RecordStore
      .GetSnapshots<EmptyAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    await RecordStore.DeleteSnapshotAsync<EmptyAggregate>(snapshot.AggregateId, snapshot.Index);

    Assert.False(await RecordStore
      .GetSnapshots<EmptyAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .AnyAsync());
  }
}
