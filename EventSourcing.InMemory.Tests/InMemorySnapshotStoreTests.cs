using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests;

public class InMemorySnapshotStoreTests : SnapshotStoreTests
{
  protected override ISnapshotStore SnapshotStore { get; }

  public InMemorySnapshotStoreTests()
  {
    SnapshotStore = new InMemorySnapshotStore();
  }
}