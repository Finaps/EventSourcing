using EventSourcing.Core.Tests.MockEventStore;

namespace EventSourcing.Core.Tests
{
    public class InMemoryEventStoreTests : EventStoreTests
    {
        public override IEventStore Store { get; }

        public InMemoryEventStoreTests()
        {
            Store = new InMemoryEventStore();
        }
    }
}