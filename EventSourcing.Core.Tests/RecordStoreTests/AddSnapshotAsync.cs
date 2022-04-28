using Finaps.EventSourcing.Core.Tests.Mocks;

namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });
    
    var factory = new SimpleSnapshotFactory();
    
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));
  }
  
  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Index()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });

    var factory = new SimpleSnapshotFactory();

    var snapshot = factory.CreateSnapshot(aggregate);
    await RecordStore.AddSnapshotAsync(snapshot);

    await Assert.ThrowsAsync<RecordStoreException>(async () => 
      await RecordStore.AddSnapshotAsync(snapshot));
  }
}