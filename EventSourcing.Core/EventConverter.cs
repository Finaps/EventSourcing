using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  /// <summary>
  /// Custom <see cref="Event"/><see cref="JsonConverter{T}"/>
  /// </summary>
  /// <remarks>
  /// Enables Polymorphic Serialization and Deserialization using the <see cref="Event"/>.<see cref="Event.Type"/> property
  /// </remarks>
  public class EventConverter<TBaseEvent> : JsonConverter<TBaseEvent> where TBaseEvent : Event
  {
    private class EventType
    {
      public string Type { get; set; }
    }
    
    /// <summary>
    /// Dictionary containing mapping between <see cref="Event"/>.<see cref="Event.Type"/> string and actual <see cref="Event"/> type
    /// </summary>
    private static readonly Dictionary<string, Type> EventTypes =
      AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(Event).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        .ToDictionary(type => type.Name);

    /// <summary>
    /// Use <see cref="EventConverter"/> for all Types inheriting from <see cref="Event"/>
    /// </summary>
    /// <param name="typeToConvert">Type to Convert</param>
    public override bool CanConvert(Type typeToConvert) =>
      typeof(Event).IsAssignableFrom(typeToConvert);

    /// <summary>
    /// Serialize Event
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TBaseEvent value, JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, value, EventTypes[value.Type]);

    /// <summary>
    /// Deserialize Event
    /// </summary>
    /// <exception cref="JsonException">Thrown when <see cref="Event"/> type cannot be found.</exception>
    public override TBaseEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var readerClone = reader;
      var typeString = JsonSerializer.Deserialize<EventType>(ref readerClone)?.Type;
      var type = EventTypes[typeString ?? throw new JsonException($"Can't decode Event with type {typeString}")];
      
      return (TBaseEvent) JsonSerializer.Deserialize(ref reader, type);
    }
  }
}