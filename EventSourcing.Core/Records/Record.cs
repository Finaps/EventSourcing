using System.Reflection;
using EventSourcing.Core.Types;

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
  public string id => GetId(AggregateId, Index);
  
  /// <summary>
  /// Create new Record
  /// </summary>
  protected Record()
  {
    if(!RecordTypeProvider.Instance.Initialized)
      RecordTypeProvider.Instance.Initialize();
    
    RecordId = Guid.NewGuid();
    Type = RecordTypeProvider.Instance.GetRecordTypeString(GetType());
    Timestamp = DateTimeOffset.Now;
  }
  
  /// <summary>
  /// Construct Database Id from <see cref="Event.AggregateId"/> and <see cref="Index"/>
  /// </summary>
  /// <param name="aggregateId">Aggregate Id</param>
  /// <param name="index">Record Index</param>
  /// <returns>Record 'id' string</returns>
  public static string GetId(Guid aggregateId, long index) => $"{aggregateId}[{index}]";
}