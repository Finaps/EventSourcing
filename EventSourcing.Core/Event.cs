using System;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public class Event : ITyped
  {
    [JsonPropertyName("id")] // To Make CosmosDB Happy
    public Guid Id { get; init; }
    
    public string Type { get; init; }
    public string AggregateType { get; init; }
    public Guid AggregateId { get; init; }
    public int AggregateVersion { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    
    public Event() { }
    public Event(Aggregate aggregate)
    {
      Id = Guid.NewGuid();
      Type = GetType().Name;
      AggregateType = aggregate.GetType().Name;
      AggregateId = aggregate.Id;
      AggregateVersion = aggregate.Version;
      Timestamp = DateTimeOffset.Now;
    }
  }
}