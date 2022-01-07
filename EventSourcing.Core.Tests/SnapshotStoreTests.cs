using EventSourcing.Core.Exceptions;
using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract class SnapshotStoreTests
{
  protected abstract ISnapshotStore SnapshotStore { get; }
  protected abstract ISnapshotStore<TBaseEvent> GetSnapshotStore<TBaseEvent>() where TBaseEvent : Event, new();

  [Fact]
  public async Task Can_Add_Snapshot()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(aggregate.CreateSnapshot());
    var snapshot = aggregate.UncommittedEvents.First();
    await SnapshotStore.AddSnapshotAsync(snapshot);
  }

  [Fact]
  public async Task Cannot_Add_Snapshot_With_Duplicate_AggregateId_And_Version()
  {
    var aggregate = new SnapshotAggregate();
    aggregate.Add(aggregate.CreateSnapshot());
    var snapshot = aggregate.UncommittedEvents.First();

    await SnapshotStore.AddSnapshotAsync(snapshot);

    await Assert.ThrowsAnyAsync<ConcurrencyException>(
      async () => await SnapshotStore.AddSnapshotAsync(snapshot));
  }
  
  [Fact]
  public async Task Can_Get_Snapshot_By_PartitionId()
  {
    var aggregate = new SnapshotAggregate { PartitionId = Guid.NewGuid() };
    aggregate.Add(aggregate.CreateSnapshot());
    await SnapshotStore.AddSnapshotAsync(aggregate.UncommittedEvents.First());

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
    aggregate.Add(aggregate.CreateSnapshot());
    await SnapshotStore.AddSnapshotAsync(aggregate.UncommittedEvents.First());

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
    aggregate.Add(aggregate.CreateSnapshot());
    aggregate.Add(new EmptyEvent());
    aggregate.Add(aggregate.CreateSnapshot());

    var snapshot = aggregate.UncommittedEvents[0];
    var snapshot2 = aggregate.UncommittedEvents[2];
    Assert.NotEqual(snapshot.AggregateVersion, snapshot2.AggregateVersion);

    await SnapshotStore.AddSnapshotAsync(snapshot);
    await SnapshotStore.AddSnapshotAsync(snapshot2);

    var result = await SnapshotStore.Snapshots
      .Where(x => x.AggregateId == aggregate.Id)
      .OrderByDescending(x => x.AggregateVersion)
      .AsAsyncEnumerable()
      .FirstOrDefaultAsync();

    Assert.NotNull(result);
    Assert.Equal(snapshot2.AggregateVersion, result.AggregateVersion);
  }
}