using EventSourcing.Core;
using EventSourcing.Core.Tests;

namespace EventSourcing.InMemory.Tests
{
  public class InMemoryEventStoreTests : EventStoreTests
  {
    protected override IEventStore GetEventStore() => new InMemoryEventStore();
    protected override IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() => new InMemoryEventStore<TBaseEvent>();
  }
}
