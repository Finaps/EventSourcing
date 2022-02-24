namespace EventSourcing.Core;

public record Projection : Record
{
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid AggregateId { get; init; }
  
  /// <summary>
  /// Aggregate type string
  /// </summary>
  public string AggregateType { get; init; }
  
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; init; }

  public string Hash { get; init; }

  public override string id => $"{Kind}|{Type}|{AggregateId}";
}