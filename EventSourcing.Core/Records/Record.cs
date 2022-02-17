using EventSourcing.Core.Services;

namespace EventSourcing.Core.Records;

public enum RecordKind { None, Event, Snapshot, Aggregate, View }

/// <summary>
/// Base Record for <see cref="Event"/>s, <see cref="Snapshot"/>s, <see cref="Aggregate"/>s and <see cref="View"/>s
/// </summary>
public record Record
{
  /// <summary>
  /// <see cref="RecordKind"/> of this Record. used to discern between Record kinds in database queries
  /// </summary>
  public RecordKind Kind => this switch
  {
    Event => RecordKind.Event,
    Snapshot => RecordKind.Snapshot,
    Aggregate => RecordKind.Aggregate,
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
  /// <see cref="ITransaction"/> and <see cref="IAggregateTransaction"/> are scoped to <see cref="PartitionId"/>
  /// </remarks>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Record identifier.
  /// </summary>
  public Guid Id { get; init; }
  
  /// <summary>
  /// Unique Database identifier.
  /// </summary>
  public virtual string id => Id.ToString();

  /// <summary>
  /// Create new Record
  /// </summary>
  protected Record()
  {
    Id = Guid.NewGuid();
    Type = RecordTypeCache.GetAssemblyRecordTypeString(GetType());
  }
}