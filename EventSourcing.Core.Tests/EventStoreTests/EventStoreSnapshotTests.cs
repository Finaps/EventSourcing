using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Cannot_Create_Snapshot_For_Aggregate_Without_Events()
  {
    var aggregate = new SnapshotAggregate();
    Assert.Throws<InvalidOperationException>(() => aggregate.CreateLinkedSnapshot());
  }
  
  [Fact]
  public async Task Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(new EmptyEvent());
    await EventStore.AddAsync(aggregate.CreateLinkedSnapshot());
  }

  [Fact]
  public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Version()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(new EmptyEvent());
    var snapshot = aggregate.CreateLinkedSnapshot();
    
    await EventStore.AddAsync(snapshot);

    await Assert.ThrowsAnyAsync<EventStoreException>(
      async () => await EventStore.AddAsync(snapshot));
  }
  
  [Fact]
  public async Task Can_Get_Snapshot_By_PartitionId()
  {
    var aggregate = new SnapshotAggregate { PartitionId = Guid.NewGuid() };
    aggregate.Add(new EmptyEvent());
    await EventStore.AddAsync(aggregate.CreateLinkedSnapshot());

    var count = await EventStore.Snapshots
      .Where(x => x.PartitionId == aggregate.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(new EmptyEvent());
    await EventStore.AddAsync(aggregate.CreateLinkedSnapshot());

    var count = await EventStore.Snapshots
      .Where(x => x.AggregateId == aggregate.RecordId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Latest_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(new EmptyEvent());
    var snapshot = aggregate.CreateLinkedSnapshot();
    aggregate.Add(new EmptyEvent());
    var snapshot2 = aggregate.CreateLinkedSnapshot();

    Assert.NotEqual(snapshot.Index, snapshot2.Index);

    await EventStore.AddAsync(snapshot);
    await EventStore.AddAsync(snapshot2);

    var result = await EventStore.Snapshots
      .Where(x => x.AggregateId == aggregate.RecordId)
      .OrderByDescending(x => x.Index)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync();

    Assert.NotNull(result);
    Assert.Equal(snapshot2.Index, result.Index);
  }
}