namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteSnapshotAsync_Can_Delete_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await GetRecordStore().AddEventsAsync(new [] { e });

    var factory = new SimpleSnapshotFactory();
    var snapshot = factory.CreateSnapshot(aggregate);
    await GetRecordStore().AddSnapshotAsync(snapshot);

    Assert.NotNull(await GetRecordStore()
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    await GetRecordStore().DeleteSnapshotAsync<SnapshotAggregate>(snapshot.AggregateId, snapshot.Index);

    Assert.False(await GetRecordStore()
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .AnyAsync());
  }
}
