using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests;

public class InMemoryEventStoreTests : EventStoreTests
{
  protected override IEventStore EventStore { get; }
  protected override IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() => new InMemoryEventStore<TBaseEvent>();

  public InMemoryEventStoreTests()
  {
    EventStore = new InMemoryEventStore();
  }
}