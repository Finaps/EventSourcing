using EventSourcing.Core.Snapshotting;

namespace EventSourcing.Core.Tests.MockAggregates
{
    public record MockSnapshot : SnapshotEvent
    {
        public int Counter;
    }
}