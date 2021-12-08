using System;

namespace EventSourcing.Core.Snapshotting
{
    public interface ISnapshottable<TBaseEvent> where TBaseEvent : Event
    {
        public uint? IntervalLength { get; }
        public uint LastSnapshotVersion { get; set; }
        public TimeSpan? IntervalDuration { get; }
        public DateTimeOffset LastSnapshotAt { get; set; }
        public TBaseEvent CreateSnapshot();
        public void UpdateLastSnapshotVersion(TBaseEvent e);
    }
}