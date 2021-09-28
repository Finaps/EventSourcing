using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public class TypedConverter<TTyped> : JsonConverter<TTyped> where TTyped : ITyped
  {
    private static readonly Dictionary<string, Type> Types =
      AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(TTyped).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        .ToDictionary(type => type.Name);

    public override bool CanConvert(Type typeToConvert) =>
      typeof(Event).IsAssignableFrom(typeToConvert);

    public override void Write(Utf8JsonWriter writer, TTyped value, JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, value, Types[value.Type]);

    public override TTyped Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var readerClone = reader;
      var typeString = JsonSerializer.Deserialize<Typed>(ref readerClone)?.Type;
      var type = Types[typeString ?? throw new JsonException($"Can't decode {typeof(TTyped).Name} with type {typeString}")];
      
      return (TTyped) JsonSerializer.Deserialize(ref reader, type);
    }
  }
}