namespace EventSourcing.Core;

/// <summary>
/// Event <see cref="Record"/>. Represents an event that happened to a particular <see cref="Aggregate"/>
/// </summary>
public record Event : Record
{
  /// <summary>
  /// Aggregate type string
  /// </summary>
  public string? AggregateType { get; init; }
  
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid AggregateId { get; init; }
  
  /// <summary>
  /// Index of this Record in the Aggregate Record Stream
  /// </summary>
  public long Index { get; init; }

  /// <summary>
  /// Record creation time
  /// </summary>
  public DateTimeOffset Timestamp { get; init; }

  /// <summary>
  /// Unique Database Identifier
  /// </summary>
  public override string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";

  /// <summary>
  /// Create new Record
  /// </summary>
  public Event() => Timestamp = DateTimeOffset.Now;
}