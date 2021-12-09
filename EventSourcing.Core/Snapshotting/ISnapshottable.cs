using System;

namespace EventSourcing.Core.Snapshotting
{
    public interface ISnapshottable
    {
        public static uint IntervalLength { get; }
        public ISnapshot CreateSnapshot();

        public bool IntervalExceeded(uint previousVersion, uint currentVersion)
        {
            var adjusted = previousVersion % IntervalLength;
            return IntervalLength > previousVersion - adjusted && IntervalLength <= currentVersion - adjusted;
        }
    }
}