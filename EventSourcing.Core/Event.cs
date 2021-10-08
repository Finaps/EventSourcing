using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public class Event
  {
    [JsonPropertyName("id")] // To Make CosmosDB Happy
    public Guid Id { get; init; }
    public string Type { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string AggregateType { get; init; }
    public Guid AggregateId { get; init; }
    public int AggregateVersion { get; init; }

    public static TEvent Create<TEvent>(Aggregate<Event> aggregate) where TEvent : Event, new() => new()
    {
      Id = Guid.NewGuid(),
      Type = typeof(TEvent).Name,
      AggregateType = aggregate.GetType().Name,
      AggregateId = aggregate.Id,
      AggregateVersion = aggregate.Version,
      Timestamp = DateTimeOffset.Now
    };
    
    public static TEvent Create<TEvent, TEventData>(Aggregate<Event> aggregate, TEventData data) where TEvent : Event, TEventData, new() =>
      Mapper.Map(data, Create<TEvent>(aggregate), MapperExclude);

    private static readonly HashSet<string> MapperExclude = new (new[]
    {
      nameof(Id), nameof(Type), nameof(Timestamp),
      nameof(AggregateType), nameof(AggregateId), nameof(AggregateVersion)
    });
  }
}