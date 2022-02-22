namespace EventSourcing.Core;

public record View : Record
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

  public override string id => $"{Kind}|{Type}|{AggregateId}";
}