using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract class SnapshotStoreTests
{
  protected abstract ISnapshotStore SnapshotStore { get; }

  [Fact]
  public async Task Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    await SnapshotStore.AddAsync(aggregate.CreateLinkedSnapshot());
  }

  [Fact]
  public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Version()
  {
    var aggregate = new SnapshotAggregate();
    var snapshot = aggregate.CreateLinkedSnapshot();
    
    await SnapshotStore.AddAsync(snapshot);

    await Assert.ThrowsAnyAsync<EventStoreException>(
      async () => await SnapshotStore.AddAsync(snapshot));
  }
  
  [Fact]
  public async Task Can_Get_Snapshot_By_PartitionId()
  {
    var aggregate = new SnapshotAggregate { PartitionId = Guid.NewGuid() };
    await SnapshotStore.AddAsync(aggregate.CreateLinkedSnapshot());

    var count = await SnapshotStore.Snapshots
      .Where(x => x.PartitionId == aggregate.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();
    await SnapshotStore.AddAsync(aggregate.CreateLinkedSnapshot());

    var count = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(1, count);
  }

  [Fact]
  public async Task Can_Get_Latest_Snapshot_By_AggregateId()
  {
    var aggregate = new SnapshotAggregate();

    var snapshot = aggregate.CreateLinkedSnapshot();
    aggregate.Add(new EmptyEvent());
    var snapshot2 = aggregate.CreateLinkedSnapshot();

    Assert.NotEqual(snapshot.AggregateVersion, snapshot2.AggregateVersion);

    await SnapshotStore.AddAsync(snapshot);
    await SnapshotStore.AddAsync(snapshot2);

    var result = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .OrderByDescending(x => x.AggregateVersion)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync();

    Assert.NotNull(result);
    Assert.Equal(snapshot2.AggregateVersion, result.AggregateVersion);
  }
}