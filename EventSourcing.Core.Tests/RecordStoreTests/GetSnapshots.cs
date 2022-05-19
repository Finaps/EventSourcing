namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Can_Get_Snapshot_By_PartitionId()
  {
    var aggregate = new SnapshotAggregate { PartitionId = Guid.NewGuid() };
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });
    
    var factory = new SimpleSnapshotFactory();
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));

    var count = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.PartitionId == aggregate.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Can_Get_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });

    var factory = new SimpleSnapshotFactory();
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));

    var count = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Can_Get_Latest_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    var e1 = aggregate.Apply(new SnapshotEvent());

    var store = RecordStore;

    await store.AddEventsAsync(new List<Event> { e1 });
    
    var factory = new SimpleSnapshotFactory();
    
    var snapshot1 = factory.CreateSnapshot(aggregate);
    
    var e2 = aggregate.Apply(new SnapshotEvent());
    await store.AddEventsAsync(new List<Event> { e2 });
    
    var snapshot2 = factory.CreateSnapshot(aggregate);

    Assert.NotEqual(snapshot1.Index, snapshot2.Index);

    await store.AddSnapshotAsync(snapshot1);
    await store.AddSnapshotAsync(snapshot2);

    var result = await RecordStore
      .GetSnapshots<SnapshotAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .OrderByDescending(x => x.Index)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync();

    Assert.NotNull(result);
    Assert.Equal(snapshot2.Index, result!.Index);
  }
}