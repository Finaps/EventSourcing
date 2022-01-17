namespace EventSourcing.Core;

public record Record
{
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
  /// Aggregate type
  /// </summary>
  public string AggregateType { get; init; }
  
  /// <summary>
  /// Record type
  /// </summary>
  public string Type { get; init; }
    
  /// <summary>
  /// Record creation time
  /// </summary>
  public DateTimeOffset Timestamp { get; init; }
  
  /// <summary>
  /// Index of this Event in the Aggregate Event Stream
  /// </summary>
  public long AggregateVersion { get; init; }

  /// <summary>
  /// Unique Database Identifier
  /// </summary>
  public string id => GetId(AggregateId, AggregateVersion);
  
  /// <summary>
  /// Create new Event
  /// </summary>
  public Record()
  {
    RecordId = Guid.NewGuid();
    Type = GetType().FullName;
    Timestamp = DateTimeOffset.Now;
  }
  
  /// <summary>
  /// Construct Database Id from <see cref="Event.AggregateId"/> and <see cref="Event.AggregateVersion"/>
  /// </summary>
  /// <param name="aggregateId"></param>
  /// <param name="aggregateVersion"></param>
  /// <returns></returns>
  public static string GetId(Guid aggregateId, long aggregateVersion) => $"{aggregateId}|{aggregateVersion}";
}