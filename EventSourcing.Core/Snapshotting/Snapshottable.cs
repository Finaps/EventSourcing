using System;

namespace EventSourcing.Core.Snapshotting
{ 
    public abstract class Snapshottable<TBaseEvent> : ISnapshottable<TBaseEvent> where TBaseEvent : Event
    {
        public uint LastSnapshotVersion { get; set; }
        public DateTimeOffset LastSnapshotAt { get; set; }

        public abstract TBaseEvent CreateSnapshot();
        
        public virtual void ApplySnapshot(TBaseEvent e)
        {
            LastSnapshotVersion = e.AggregateVersion;
            LastSnapshotAt = e.Timestamp;
        }
    }
}