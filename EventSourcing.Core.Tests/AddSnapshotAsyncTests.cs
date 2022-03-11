using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task Cannot_Create_Snapshot_For_Aggregate_Without_Events()
  {
    var aggregate = new SnapshotAggregate();
    var factory = new SimpleSnapshotFactory();

    Assert.Throws<InvalidOperationException>(() => factory.CreateSnapshot(aggregate));
  }
  
  [Fact]
  public async Task Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Apply(new EmptyEvent());
    
    var factory = new SimpleSnapshotFactory();
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));
  }

  [Fact]
  public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Index()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Apply(new EmptyEvent());

    var factory = new SimpleSnapshotFactory();

    var snapshot = factory.CreateSnapshot(aggregate);
    await RecordStore.AddSnapshotAsync(snapshot);

    await Assert.ThrowsAsync<RecordStoreException>(async () => 
      await RecordStore.AddSnapshotAsync(snapshot));
  }
  
  [Fact]
  public async Task Can_Get_Snapshot_By_PartitionId()
  {
    var aggregate = new SnapshotAggregate { PartitionId = Guid.NewGuid() };
    aggregate.Apply(new EmptyEvent());
    
    var factory = new SimpleSnapshotFactory();
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));

    var count = await RecordStore.Snapshots
      .Where(x => x.PartitionId == aggregate.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Apply(new EmptyEvent());

    var factory = new SimpleSnapshotFactory();
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));

    var count = await RecordStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Latest_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Apply(new EmptyEvent());
    
    var factory = new SimpleSnapshotFactory();
    
    var snapshot = factory.CreateSnapshot(aggregate);
    aggregate.Apply(new EmptyEvent());
    var snapshot2 = factory.CreateSnapshot(aggregate);

    Assert.NotEqual(snapshot.Index, snapshot2.Index);

    await RecordStore.AddSnapshotAsync(snapshot);
    await RecordStore.AddSnapshotAsync(snapshot2);

    var result = await RecordStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .OrderByDescending(x => x.Index)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync();

    Assert.NotNull(result);
    Assert.Equal(snapshot2.Index, result!.Index);
  }
}