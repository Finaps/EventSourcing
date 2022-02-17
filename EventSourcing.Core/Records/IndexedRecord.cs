namespace EventSourcing.Core.Records;

/// <summary>
/// Base Indexed Record, representing the common properties between <see cref="Event"/>s and <see cref="Snapshot"/>s.
/// </summary>
public record IndexedRecord : Record
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
  protected IndexedRecord() => Timestamp = DateTimeOffset.Now;
}