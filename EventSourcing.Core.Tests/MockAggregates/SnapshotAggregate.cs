using EventSourcing.Core.Snapshotting;

namespace EventSourcing.Core.Tests.MockAggregates
{
    public class SnapshotAggregate : Aggregate<Event>, ISnapshottable
    {
        public uint IntervalLength => 100;
        public int Counter;
        public int NumberOfEventsApplied;
        public int NumberOfSnapshotsApplied;
        protected override void Apply<TEvent>(TEvent e)
        {
            switch (e)
            {
                case EmptyEvent:
                    NumberOfEventsApplied++;
                    Counter++;
                    break;
                case MockSnapshot snapshot:
                    NumberOfSnapshotsApplied++;
                    Counter = snapshot.Counter;
                    break;
            }
        }


        public ISnapshot CreateSnapshot()
        {
            return new MockSnapshot{Counter = this.Counter};
        }
    }
}