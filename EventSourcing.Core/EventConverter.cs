using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public class EventType
  {
    public string Type { get; set; }
  }
  
  public class EventConverter : JsonConverter<Event>
  {
    private static readonly Dictionary<string, Type> EventTypes =
      AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(Event).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        .ToDictionary(type => type.Name);

    public override bool CanConvert(Type typeToConvert) =>
      typeof(Event).IsAssignableFrom(typeToConvert);

    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, value, EventTypes[value.Type]);

    public override Event Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var readerClone = reader;
      var typeString = JsonSerializer.Deserialize<EventType>(ref readerClone)?.Type;
      var type = EventTypes[typeString ?? throw new JsonException($"Can't decode Event with type {typeString}")];
      
      return (Event) JsonSerializer.Deserialize(ref reader, type);
    }
  }
}