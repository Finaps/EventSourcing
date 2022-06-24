using System.Reflection;
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
  private static readonly Dictionary<Type, string> RecordTypeStrings =
    EventSourcingCache.RecordTypes.ToDictionary(
      type => type, type => type.GetCustomAttribute<RecordTypeAttribute>()?.Type ?? type.Name);

  private static readonly Dictionary<string, Type> RecordStringTypes =
    RecordTypeStrings.ToDictionary(x => x.Value, x => x.Key);

  private static readonly Dictionary<Type, PropertyInfo[]> NonNullableRecordProperties = EventSourcingCache.RecordTypes
    .ToDictionary(type => type, type => type.GetProperties().Where(property =>
      property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
  
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
  public override void Write(Utf8JsonWriter writer, TRecord value, JsonSerializerOptions options)
  {
    if (!RecordTypeStrings.TryGetValue(value.GetType(), out var type))
      throw new ArgumentException($"Couldn't find Record Type string for {value.GetType()}. " +
                                  "Make sure the type is a public non-abstract record.", nameof(value));
      
    JsonSerializer.Serialize(writer, value with { Type = type }, value.GetType());
  }

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
    JsonSerializer.Deserialize(ref reader, DeserializeRecordType(reader)) as TRecord 
           ?? throw new JsonException($"Error Converting Json to {typeToConvert}.");

  private Type DeserializeRecordType(Utf8JsonReader reader)
  {
    // TODO: Optimize this by only querying the first field
    var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader);
    
    // Get Record.Type String from Json
    if (json == null || !json.TryGetValue(nameof(Record.Type), out var typeString) || typeString.ValueKind != JsonValueKind.String)

                     // Throw Exception when json has no "Type" Property
                     throw new RecordValidationException(
                       $"Error converting {typeof(TRecord)}. " +
                       $"Couldn't parse {typeof(TRecord)}.{nameof(Record.Type)} from Json. " +
                       $"Does the Json document contain a {nameof(Record.Type)} field?");

    if (!RecordStringTypes.TryGetValue(typeString.GetString()!, out var type))
      throw new ArgumentException($"Couldn't find Record Type string for {typeString}. " +
                                  "Make sure the type is a public non-abstract record.");

    if (!_throwOnMissingNonNullableProperties || !NonNullableRecordProperties.TryGetValue(type, out var properties)) 
      return type;
    
    var missing = properties
      .Where(property => !json.TryGetValue(property.Name, out var value) || value.ValueKind == JsonValueKind.Null)
      .Select(property => property.Name)
      .ToList();
    
    if (missing.Count > 0)
      throw new RecordValidationException(
        $"Error converting Json to {type}'.\n" +
        $"One ore more non-nullable properties are missing or null: {string.Join(", ", missing.Select(property => $"{type.Name}.{property}"))}.\n" +
        $"Either make properties nullable or use a RecordMigrator to handle {typeof(TRecord)} versioning.");
    
    return type;
  }
}