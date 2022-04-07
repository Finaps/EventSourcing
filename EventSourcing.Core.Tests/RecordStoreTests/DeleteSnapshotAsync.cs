namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_DeleteSnapshotAsync_Can_Delete_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });

    var factory = new SimpleSnapshotFactory();
    var snapshot = factory.CreateSnapshot(aggregate);
    await RecordStore.AddSnapshotAsync(snapshot);

    Assert.NotNull(await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .SingleAsync());

    await RecordStore.DeleteSnapshotAsync<SnapshotAggregate>(snapshot.AggregateId, snapshot.Index);

    Assert.False(await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == snapshot.AggregateId)
      .AsAsyncEnumerable()
      .AnyAsync());
  }
}
