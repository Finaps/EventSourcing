namespace EventSourcing.Core;

public interface ISnapshottable
{
    public uint IntervalLength { get; }
    public SnapshotEvent CreateSnapshot();

    public bool IntervalExceeded<TBaseEvent>()
        where TBaseEvent: Event
    {
        if (this is not Aggregate<TBaseEvent> aggregate)
            throw new InvalidOperationException($"Cannot check aggregate version of type {GetType()}");
            
        var previousVersion = aggregate.UncommittedEvents.FirstOrDefault()?.AggregateVersion ?? 0;
        var currentVersion = aggregate.Version;
        var adjusted = previousVersion - previousVersion % IntervalLength;
        return IntervalLength <= currentVersion - adjusted;
    }
}