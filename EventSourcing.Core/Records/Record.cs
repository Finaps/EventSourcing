namespace EventSourcing.Core;

public enum RecordKind { None, Event, Snapshot, Projection }

/// <summary>
/// Base Record for <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
public abstract record Record
{
  /// <summary>
  /// <see cref="RecordKind"/> of this Record. used to discern between Record kinds in database queries
  /// </summary>
  public RecordKind Kind => this switch
  {
    Projection => RecordKind.Projection,
    Snapshot => RecordKind.Snapshot,
    Event => RecordKind.Event,
    _ => RecordKind.None
  };
  
  /// <summary>
  /// String representation of Record Type
  /// </summary>
  /// <remarks>
  /// Can be overridden using <see cref="RecordTypeAttribute"/>
  /// </remarks>
  public string Type { get; init; }
  
  /// <summary>
  /// Unique Partition identifier.
  /// </summary>
  /// <remarks>
  /// <see cref="IRecordTransaction"/> and <see cref="IAggregateTransaction"/> are scoped to <see cref="PartitionId"/>
  /// </remarks>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Aggregate type string
  /// </summary>
  public string? AggregateType { get; init; }
  
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid AggregateId { get; init; }
  
  /// <summary>
  /// Unique Record identifier.
  /// </summary>
  public Guid RecordId { get; init; }
  
  /// <summary>
  /// Record creation/update time
  /// </summary>
  public DateTimeOffset Timestamp { get; init; }
  
  /// <summary>
  /// Unique Database identifier.
  /// </summary>
  public abstract string id { get; }

  /// <summary>
  /// Create new Record
  /// </summary>
  protected Record()
  {
    RecordId = Guid.NewGuid();
    Type = RecordTypeCache.GetAssemblyRecordTypeString(GetType());
    Timestamp = DateTimeOffset.Now;
  }
}