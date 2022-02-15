namespace EventSourcing.Core;

public record Record
{
  public RecordKind Kind => this switch
  {
    Event => RecordKind.Event,
    Snapshot => RecordKind.Snapshot,
    _ => RecordKind.None
  };
  
  /// <summary>
  /// Unique Partition identifier
  /// </summary>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid AggregateId { get; init; }
  
  /// <summary>
  /// Unique Record identifier
  /// </summary>
  public Guid RecordId { get; init; }
  
  /// <summary>
  /// Index of this Record in the Aggregate Record Stream
  /// </summary>
  public long Index { get; init; }
  
  /// <summary>
  /// Aggregate type
  /// </summary>
  public string? AggregateType { get; init; }

  /// <summary>
  /// Record type
  /// </summary>
  public string Type { get; init; }
    
  /// <summary>
  /// Record creation time
  /// </summary>
  public DateTimeOffset Timestamp { get; init; }

  /// <summary>
  /// Unique Database Identifier
  /// </summary>
  public string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";

  /// <summary>
  /// Create new Record
  /// </summary>
  protected Record()
  {
    RecordId = Guid.NewGuid();
    Type = RecordTypeCache.GetAssemblyRecordTypeString(GetType());
    Timestamp = DateTimeOffset.Now;
  }

  public string Format()
  {
    var partitionId = PartitionId == Guid.Empty ? "" : $"{nameof(PartitionId)} = {PartitionId}, ";
    var aggregateId = $"{nameof(AggregateId)} = {AggregateId}, ";
    var index = $"{nameof(Index)} = {Index}";
    return $"{Type} {{ {partitionId}{aggregateId}{index} }}";
  } 
}