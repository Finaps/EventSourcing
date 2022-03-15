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
    Json = JsonDocument.Parse(JsonSerializer.Serialize(e, _options))
  };
  
  public SnapshotEntity Serialize(Snapshot s) => new()
  {
    PartitionId = s.PartitionId,
    AggregateId = s.AggregateId,

    AggregateType = s.AggregateType,
    Type = s.Type,

    Index = s.Index,
    Timestamp = s.Timestamp,
    Json = JsonDocument.Parse(JsonSerializer.Serialize(s, _options))
  };

  public TResult Deserialize<TResult>(EventEntity? e) => e.Json.Deserialize<TResult>(_options)!;
  public TResult Deserialize<TResult>(SnapshotEntity? s) => s.Json.Deserialize<TResult>(_options)!;
}