namespace EventSourcing.Core;

/// <summary>
/// Event <see cref="Record"/>. Represents an event that happened to a particular <see cref="Aggregate"/>
/// </summary>
public record Event : Record
{
  /// <summary>
  /// Index of this Record in the Aggregate Record Stream
  /// </summary>
  public long Index { get; init; }

  /// <summary>
  /// Unique Database Identifier
  /// </summary>
  public override string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";
}