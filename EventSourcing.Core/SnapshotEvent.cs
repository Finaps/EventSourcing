namespace EventSourcing.Core;

public record SnapshotEvent : Event, ISnapshot;