namespace EventSourcing.Core;

public record Event
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
  /// Aggregate type
  /// </summary>
  public string AggregateType { get; init; }

  /// <summary>
  /// Unique Event identifier
  /// </summary>
  public Guid EventId { get; init; }
  
  /// <summary>
  /// Event type
  /// </summary>
  public string Type { get; init; }
    
  /// <summary>
  /// Event creation time
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
  public Event()
  {
    EventId = Guid.NewGuid();
    Type = GetType().Name;
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