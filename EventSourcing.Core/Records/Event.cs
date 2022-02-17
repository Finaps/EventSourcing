namespace EventSourcing.Core.Records;

/// <summary>
/// Event <see cref="Record"/>. Represents an event that happened to a particular <see cref="Aggregate"/>
/// </summary>
public record Event : IndexedRecord;