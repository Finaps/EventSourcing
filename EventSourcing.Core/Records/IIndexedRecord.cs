namespace EventSourcing.Core.Records;

public interface IIndexedRecord : IRecord
{
  /// <summary>
  /// Aggregate type string
  /// </summary>
  public string? AggregateType { get; }
  
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid AggregateId { get; }
  
  /// <summary>
  /// Index of this Record in the Aggregate Record Stream
  /// </summary>
  public long Index { get; }

  /// <summary>
  /// Record creation time
  /// </summary>
  public DateTimeOffset Timestamp { get; }
}