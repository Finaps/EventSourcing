using EventSourcing.Core.Services;

namespace EventSourcing.Core.Records;

public enum RecordKind { None, Event, Snapshot, Aggregate }

/// <summary>
/// Base Record for <see cref="Event"/>s, <see cref="Snapshot"/>s, <see cref="Aggregate"/>s and <see cref="View"/>s
/// </summary>
public record Record : IRecord
{
  public RecordKind Kind => this switch
  {
    Event => RecordKind.Event,
    Snapshot => RecordKind.Snapshot,
    Aggregate => RecordKind.Aggregate,
    _ => RecordKind.None
  };
  
  public string Type { get; init; }
  public Guid PartitionId { get; init; }
  public Guid Id { get; init; }
  public virtual string id => Id.ToString();
  
  protected Record()
  {
    Id = Guid.NewGuid();
    Type = RecordTypeCache.GetAssemblyRecordTypeString(GetType());
  }
}