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
    var e = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e });
    
    var factory = new SimpleSnapshotFactory();
    
    await RecordStore.AddSnapshotAsync(factory.CreateSnapshot(aggregate));
  }

  [Fact]
  public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Index()
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
  
  [Fact]
  public async Task Can_Get_Snapshot_By_PartitionId()
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
  public async Task Can_Get_Snapshot_By_AggregateId()
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
  public async Task Can_Get_Latest_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    var e1 = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e1 });
    
    var factory = new SimpleSnapshotFactory();
    
    var snapshot1 = factory.CreateSnapshot(aggregate);
    
    var e2 = aggregate.Apply(new SnapshotEvent());
    await RecordStore.AddEventsAsync(new List<Event> { e2 });
    
    var snapshot2 = factory.CreateSnapshot(aggregate);

    Assert.NotEqual(snapshot1.Index, snapshot2.Index);

    await RecordStore.AddSnapshotAsync(snapshot1);
    await RecordStore.AddSnapshotAsync(snapshot2);

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