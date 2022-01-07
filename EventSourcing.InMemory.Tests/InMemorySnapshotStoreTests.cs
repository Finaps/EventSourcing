using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests;

public class InMemorySnapshotStoreTests : SnapshotStoreTests
{
  protected override ISnapshotStore SnapshotStore { get; }
  protected override ISnapshotStore<TBaseEvent> GetSnapshotStore<TBaseEvent>() =>
    new InMemorySnapshotStore<TBaseEvent>();

  public InMemorySnapshotStoreTests()
  {
    SnapshotStore = new InMemorySnapshotStore();
  }
}