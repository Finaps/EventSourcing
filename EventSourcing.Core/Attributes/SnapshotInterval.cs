using System;

namespace EventSourcing.Core.Attributes
{
    public class SnapshotInterval : Attribute
    {
        private TimeSpan? IntervalDuration { get; }
        private int? IntervalLength { get;}

        public SnapshotInterval(TimeSpan intervalDuration)
        {
            IntervalDuration = intervalDuration;
        }
        public SnapshotInterval(int intervalLength)
        {
            IntervalLength = intervalLength;
        }
        public SnapshotInterval(TimeSpan intervalDuration, int intervalLength)
        {
            IntervalDuration = intervalDuration;
            IntervalLength = intervalLength;
        }
        public bool IntervalExceeded(TimeSpan timeSinceLastSnapshot, int eventsSinceLastSnapshot)
        {
            if (timeSinceLastSnapshot > IntervalDuration)
                return true;

            return eventsSinceLastSnapshot > IntervalLength;
        }
    }
}