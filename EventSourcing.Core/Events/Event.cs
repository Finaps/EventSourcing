namespace EventSourcing.Core;

/// <summary>
/// Base Event
/// </summary>
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
  public ulong AggregateVersion { get; init; }

  public string id => GetId(AggregateId, AggregateVersion);
    
  public Event()
  {
    EventId = Guid.NewGuid();
    Type = GetType().Name;
    Timestamp = DateTimeOffset.Now;
  }
  
  public static string GetId(Guid aggregateId, ulong aggregateVersion) => $"{aggregateId}|{aggregateVersion}";
}