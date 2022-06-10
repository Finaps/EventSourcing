using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finaps.EventSourcing.Core;

/// <summary>
/// Custom <see cref="Record"/><see cref="JsonConverter{T}"/>
/// </summary>
/// <remarks>
/// Enables Polymorphic Serialization and Deserialization using the <see cref="Record"/>.<see cref="Record.Type"/> property
/// </remarks>
public class RecordConverter<TRecord> : JsonConverter<TRecord> where TRecord : Record
{
  private readonly RecordTypeCache _recordTypeCache;
  private readonly bool _throwOnMissingNonNullableProperties;

  /// <summary>
  /// Create <see cref="RecordConverter{TRecord}"/>
  /// </summary>
  /// <param name="options">
  /// Optional <see cref="RecordConverterOptions"/> to override the <see cref="Record"/> types
  /// that are used for deserialization and migrating.
  /// </param>
  public RecordConverter(RecordConverterOptions? options = null)
  {
    _recordTypeCache = new RecordTypeCache(options?.RecordTypes);
    _throwOnMissingNonNullableProperties = options?.ThrowOnMissingNonNullableProperties ?? false;
  }

  /// <summary>
  /// Use <see cref="RecordConverter{TRecord}"/> for all Types inheriting from <see cref="Record"/>
  /// </summary>
  /// <param name="typeToConvert">Type to Convert</param>
  public override bool CanConvert(Type typeToConvert) => typeof(TRecord).IsAssignableFrom(typeToConvert);

  /// <summary>
  /// Serialize Record
  /// </summary>
  public override void Write(Utf8JsonWriter writer, TRecord value, JsonSerializerOptions options) =>
    JsonSerializer.Serialize(writer, value with { Type = RecordTypeCache.GetAssemblyRecordTypeString(value.GetType()) }, value.GetType());

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
    JsonSerializer.Deserialize(ref reader, DeserializeRecordType(reader)) as TRecord 
           ?? throw new JsonException($"Error Converting Json to {typeToConvert}.");

  private Type DeserializeRecordType(Utf8JsonReader reader)
  {
    if (reader.TokenType == JsonTokenType.StartObject &&
        reader.Read() && reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == nameof(Record.Type) &&
        reader.Read() && reader.TokenType == JsonTokenType.String)
      return _recordTypeCache.GetRecordType(reader.GetString()!);

    throw new JsonException("Could not deserialize Record Type");
  }
}