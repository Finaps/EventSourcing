using EventSourcing.Core.Snapshotting;

namespace EventSourcing.Core.Tests.MockAggregates
{
    public class SnapshotAggregate : Aggregate<Event>, ISnapshottable
    {
        public uint IntervalLength => 10;
        public int Counter;
        public int EventsAppliedAfterHydration;
        public int SnapshotsAppliedAfterHydration;
        protected override void Apply<TEvent>(TEvent e)
        {
            switch (e)
            {
                case EmptyEvent:
                    EventsAppliedAfterHydration++;
                    Counter++;
                    break;
                case MockSnapshot snapshot:
                    SnapshotsAppliedAfterHydration++;
                    Counter = snapshot.Counter;
                    break;
            }
        }
        
        public SnapshotEvent CreateSnapshot()
        {
            return new MockSnapshot{Counter = this.Counter};
        }
    }
}