namespace EventSourcing.Core;

/// <summary>
/// Represents an <see cref="Event"/> that happened to an <see cref="Aggregate"/>.
/// </summary>
/// <seealso cref="Aggregate"/>
/// <seealso cref="IRecordStore"/>
public record Event : Record
{
  /// <summary>
  /// The index of this <see cref="Event"/> with respect to <see cref="Record.AggregateId"/>
  /// </summary>
  public long Index { get; init; }

  /// <summary>
  /// Unique Database identifier
  /// </summary>
  public override string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";

  /// <summary>
  /// Create new <see cref="Event"/>
  /// </summary>
  public Event() { }
}