namespace EventSourcing.Core.Snapshotting
{
    public record SnapshotEvent : Event, ISnapshot;
}