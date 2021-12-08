using System;

namespace EventSourcing.Core.Snapshotting
{
    public interface ISnapshottable<TBaseEvent> where TBaseEvent : Event
    {
        public static uint IntervalLength { get; }
        public uint LastSnapshotVersion { get; set; }
        public static TimeSpan IntervalDuration { get; }
        public DateTimeOffset LastSnapshotAt { get; set; }
        public TBaseEvent CreateSnapshot();
        public void ApplySnapshot(TBaseEvent e);
    }
}