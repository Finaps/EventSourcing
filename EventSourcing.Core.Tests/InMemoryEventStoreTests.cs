using EventSourcing.Core.Tests.MockEventStore;

namespace EventSourcing.Core.Tests
{
  public class InMemoryEventStoreTests : EventStoreTests
  {
    protected override IEventStore GetEventStore() => new InMemoryEventStore();
    protected override IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() => new InMemoryEventStore<TBaseEvent>();
  }
}