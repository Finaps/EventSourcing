using EventSourcing.Core.Snapshotting;

namespace EventSourcing.Core.Tests.MockAggregates
{
    public record MockSnapshot : Event, ISnapshot
    {
        public int Counter;
    }
}