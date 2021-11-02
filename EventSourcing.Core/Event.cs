using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  /// <summary>
  /// Base Event
  /// </summary>
  public class Event
  {
    /// <summary>
    /// Unique Event identifier
    /// </summary>
    [JsonPropertyName("id")] // To Make CosmosDB Happy
    public Guid Id { get; init; }
    
    /// <summary>
    /// Event type
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// Unique Aggregate identifier
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Aggregate type
    /// </summary>
    public string AggregateType { get; init; }
    
    /// <summary>
    /// Index of this Event in the Aggregate Event Stream
    /// </summary>
    public int AggregateVersion { get; init; }
    
    /// <summary>
    /// Event creation time
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Create new Event
    /// </summary>
    /// <param name="aggregate">Target <see cref="Aggregate{TBaseEvent}"/></param>
    /// <typeparam name="TEvent">Event Type</typeparam>
    /// <returns><see cref="TEvent"/></returns>
    public static TEvent Create<TEvent>(Aggregate<Event> aggregate) where TEvent : Event, new()
    {
      return new TEvent
      {
        Id = Guid.NewGuid(),
        Type = typeof(TEvent).Name,
        AggregateId = aggregate.Id,
        AggregateType = aggregate.Type,
        AggregateVersion = aggregate.Version,
        Timestamp = DateTimeOffset.Now
      };
    }
    
    /// <summary>
    /// Create new Event with Data
    /// </summary>
    /// <param name="aggregate">Target <see cref="Aggregate{TBaseEvent}"/></param>
    /// <param name="data">Event Data</param>
    /// <returns><see cref="TEvent"/></returns>
    public static TEvent Create<TEvent>(Aggregate<Event> aggregate, TEvent data)
      where TEvent : Event, new()
    {
      return Mapper.Map(data, Create<TEvent>(aggregate), MapperExclude);
    }

    /// <summary>
    /// Create new Event based on TEventData
    /// </summary>
    /// <param name="aggregate">Target <see cref="Aggregate{TBaseEvent}"/></param>
    /// <param name="data">Event Data</param>
    /// <typeparam name="TEvent">Event Type, must extend <see cref="TEventData"/></typeparam>
    /// <typeparam name="TEventData">Event Data Type</typeparam>
    /// <returns><see cref="TEvent"/></returns>
    public static TEvent Create<TEvent, TEventData>(Aggregate<Event> aggregate, TEventData data)
      where TEvent : Event, TEventData, new()
    {
      return Mapper.Map(data, Create<TEvent>(aggregate), MapperExclude);
    }
    
    private static readonly HashSet<string> MapperExclude = new (new[]
    {
      nameof(Id), nameof(Type), nameof(Timestamp),
      nameof(AggregateType), nameof(AggregateId), nameof(AggregateVersion)
    });
  }
}