namespace EventSourcing.Core;

public enum RecordKind { None, Event, Snapshot, View }

/// <summary>
/// Base Record for <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="View"/>s
/// </summary>
public abstract record Record
{
  /// <summary>
  /// <see cref="RecordKind"/> of this Record. used to discern between Record kinds in database queries
  /// </summary>
  public RecordKind Kind => this switch
  {
    Snapshot => RecordKind.Snapshot,
    Event => RecordKind.Event,
    View => RecordKind.View,
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
  /// Unique Record identifier.
  /// </summary>
  public Guid RecordId { get; init; }
  
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
  }
}