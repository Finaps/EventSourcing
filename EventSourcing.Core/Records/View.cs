namespace EventSourcing.Core;

public record View : Record
{
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; init; }
  
  /// <summary>
  /// Aggregate type string
  /// </summary>
  public string AggregateType { get; init; }

  public override string id => $"{Kind}|{Type}|{Id}";
}