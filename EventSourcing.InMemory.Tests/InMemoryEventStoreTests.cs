using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests;

public class InMemoryEventStoreTests : EventStoreTests
{
  protected override IEventStore EventStore { get; }

  public InMemoryEventStoreTests()
  {
    EventStore = new InMemoryEventStore();
  }
}