namespace EventSourcing.Core;

/// <summary>
/// Snapshot <see cref="Record"/>. Represents a snapshot of an <see cref="Aggregate"/> at a particular <see cref="Snapshot.Index"/>
/// </summary>
public record Snapshot : Event;