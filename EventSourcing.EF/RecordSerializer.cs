using System.Text.Json;
using EventSourcing.Core;

namespace EventSourcing.EF;

public class RecordSerializer
{
  private readonly JsonSerializerOptions _options;

  public RecordSerializer(JsonSerializerOptions options)
  {
    _options = options;
  }

  public EventEntity Serialize(Event e) => new()
  {
    PartitionId = e.PartitionId,
    AggregateId = e.AggregateId,

    AggregateType = e.AggregateType,
    Type = e.Type,

    Index = e.Index,
    Timestamp = e.Timestamp,
    Json = JsonSerializer.Serialize(e, _options)
  };
  
  public SnapshotEntity Serialize(Snapshot s) => new()
  {
    PartitionId = s.PartitionId,
    AggregateId = s.AggregateId,

    AggregateType = s.AggregateType,
    Type = s.Type,

    Index = s.Index,
    Timestamp = s.Timestamp,
    Json = JsonSerializer.Serialize(s, _options)
  };

  public TResult Deserialize<TResult>(EventEntity? e) => JsonSerializer.Deserialize<TResult>(e.Json, _options)!;
  public TResult Deserialize<TResult>(SnapshotEntity? s) => JsonSerializer.Deserialize<TResult>(s.Json, _options)!;
}