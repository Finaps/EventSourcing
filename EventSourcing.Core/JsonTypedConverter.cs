using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core
{
  /// <summary>
  /// Custom Polymorphic <see cref="JsonConverter{T}"/>
  /// </summary>
  /// <remarks>
  /// Enables Polymorphic Serialization and Deserialization using the <see cref="ITyped"/>.<see cref="ITyped.Type"/> property
  /// </remarks>
  public class JsonTypedConverter<TTyped> : JsonConverter<TTyped> where TTyped : ITyped
  {
    private class Typed : ITyped
    {
      public string Type { get; init; }
    }
    
    /// <summary>
    /// Dictionary containing mapping between <see cref="ITyped"/>.<see cref="ITyped.Type"/> string and actual <see cref="ITyped"/> type
    /// </summary>
    private static readonly Dictionary<string, Type> Types =
      AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => typeof(Event).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        .ToDictionary(type => type.FullName);

    /// <summary>
    /// Use <see cref="JsonTypedConverter{TTyped}"/> for all Types inheriting from <see cref="ITyped"/>
    /// </summary>
    /// <param name="typeToConvert">Type to Convert</param>
    public override bool CanConvert(Type typeToConvert) =>
      typeof(Event).IsAssignableFrom(typeToConvert);

    /// <summary>
    /// Serialize Event
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TTyped value, JsonSerializerOptions options) =>
      JsonSerializer.Serialize(writer, value, Types[value.Type]);

    /// <summary>
    /// Deserialize Event
    /// </summary>
    /// <exception cref="JsonException">Thrown when <see cref="ITyped"/> type cannot be found.</exception>
    public override TTyped Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var readerClone = reader;
      var typeString = JsonSerializer.Deserialize<Typed>(ref readerClone)?.Type;

      if (string.IsNullOrWhiteSpace(typeString))
        throw new JsonTypedConverterException($"Couldn't decode ${nameof(TTyped)}: Couldn't deserialize '{nameof(TTyped)}.Type' property");

      if (!Types.TryGetValue(typeString, out var type))
        throw new JsonTypedConverterException($"Couldn't find ${nameof(TTyped)} '{typeString}' in assembly");
      
      return (TTyped) JsonSerializer.Deserialize(ref reader, type);
    }
  }
}