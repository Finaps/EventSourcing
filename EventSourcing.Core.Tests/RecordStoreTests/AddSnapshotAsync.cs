namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await GetRecordStore().AddEventsAsync(new [] { e });
    
    var factory = new SimpleSnapshotFactory();
    
    await GetRecordStore().AddSnapshotAsync(factory.CreateSnapshot(aggregate));
  }
  
  [Fact]
  public async Task RecordStore_AddSnapshotAsync_Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Index()
  {
    var aggregate = new SnapshotAggregate();
    var e = aggregate.Apply(new SnapshotEvent());
    await GetRecordStore().AddEventsAsync(new [] { e });

    var factory = new SimpleSnapshotFactory();

    var snapshot = factory.CreateSnapshot(aggregate);
    await GetRecordStore().AddSnapshotAsync(snapshot);

    await Assert.ThrowsAsync<RecordStoreException>(async () => 
      await GetRecordStore().AddSnapshotAsync(snapshot));
  }
}