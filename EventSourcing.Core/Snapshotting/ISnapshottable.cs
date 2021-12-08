using System;

namespace EventSourcing.Core.Snapshotting
{
    public interface ISnapshottable<TBaseEvent> where TBaseEvent : Event
    {
    public uint? IntervalLength { get; }
    public uint LastSnapshotVersion { get; set; }
    public TimeSpan? IntervalDuration { get; }
    public DateTimeOffset LastSnapshotAt { get; set; }

    public abstract TBaseEvent CreateSnapshot();

    public virtual void UpdateLastSnapshotVersion(TBaseEvent e)
    {
        LastSnapshotVersion = e.AggregateVersion;
        LastSnapshotAt = e.Timestamp;
    }

    public bool IntervalExceeded(uint currentVersion)
    {
        var timeSinceLastSnapshot = DateTimeOffset.Now - LastSnapshotAt;
        var eventsSinceLastSnapshot = currentVersion - LastSnapshotVersion;

        return IntervalExceeded(timeSinceLastSnapshot, eventsSinceLastSnapshot);
    }

    private bool IntervalExceeded(TimeSpan timeSinceLastSnapshot, uint eventsSinceLastSnapshot)
    {
        if (timeSinceLastSnapshot > IntervalDuration)
            return true;

        return eventsSinceLastSnapshot > IntervalLength;
    }

    }
}