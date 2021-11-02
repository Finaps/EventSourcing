using EventSourcing.Core.Tests.MockEventStore;

namespace EventSourcing.Core.Tests
{
  public class InMemoryEventStoreTests : EventStoreTests
  {
    public override IEventStore GetEventStore() => new InMemoryEventStore();
    public override IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() => new InMemoryEventStore<TBaseEvent>();
  }
}