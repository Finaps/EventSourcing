namespace EventSourcing.Core;

public enum RecordKind { None, Event, Snapshot, Aggregate }

public record Record
{
  public RecordKind Kind => this switch
  {
    Event => RecordKind.Event,
    Snapshot => RecordKind.Snapshot,
    Aggregate => RecordKind.Aggregate,
    _ => RecordKind.None
  };
  
  /// <summary>
  /// Record type
  /// </summary>
  public string Type { get; init; }
  
  /// <summary>
  /// Unique Partition identifier
  /// </summary>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Record identifier
  /// </summary>
  public Guid Id { get; init; }
  
  /// <summary>
  /// Unique Database Identifier
  /// </summary>
  public virtual string id => Id.ToString();

  protected Record()
  {
    Id = Guid.NewGuid();
    Type = RecordTypeCache.GetAssemblyRecordTypeString(GetType());
  }
}