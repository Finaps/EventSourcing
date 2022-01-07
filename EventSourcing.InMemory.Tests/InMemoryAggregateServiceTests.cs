using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests;

public class InMemoryAggregateServiceTests : AggregateServiceTests
{
  protected override IEventStore EventStore { get; }
  protected override ISnapshotStore SnapshotStore { get; }
  protected override IAggregateService AggregateService { get; }

  public InMemoryAggregateServiceTests()
  {
    EventStore = new InMemoryEventStore();
    SnapshotStore = new InMemorySnapshotStore();
    AggregateService = new AggregateService(EventStore, SnapshotStore, null);
  }
}
