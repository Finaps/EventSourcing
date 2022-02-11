using EventSourcing.Core.Migrations;
using EventSourcing.Core.Types;

namespace EventSourcing.Core;

/// <summary>
/// Custom <see cref="Record"/><see cref="JsonConverter{T}"/>
/// </summary>
/// <remarks>
/// Enables Polymorphic Serialization and Deserialization using the <see cref="Record"/>.<see cref="Record.Type"/> property
/// </remarks>
public class RecordConverter : JsonConverter<Record>
{
  private class RecordType
  {
    public string Type { get; set; }
  }

  private readonly RecordTypeCache _recordTypeCache;
  private readonly RecordMigratorService _recordMigratorService;

  public RecordConverter(RecordConverterOptions? options = null)
  {
    _recordTypeCache = new RecordTypeCache(options?.RecordTypes);
    _recordMigratorService = new RecordMigratorService(options);
  }

  /// <summary>
  /// Use <see cref="RecordConverter"/> for all Types inheriting from <see cref="Record"/>
  /// </summary>
  /// <param name="typeToConvert">Type to Convert</param>
  public override bool CanConvert(Type typeToConvert) => typeof(Record).IsAssignableFrom(typeToConvert);

  /// <summary>
  /// Serialize Record
  /// </summary>
  public override void Write(Utf8JsonWriter writer, Record value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();
    var type = value.GetType();
    var record = Validate(value, type);
    JsonSerializer.Serialize(writer, record, type);
  }

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override Record Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var type = DeserializeRecordType(reader);
    var record = JsonSerializer.Deserialize(ref reader, type) as Record;
    return _recordMigratorService.Migrate(Validate(record, type));
  }

  private Type DeserializeRecordType(Utf8JsonReader reader)
  {
    // Get RecordType String from Json
    var typeString = JsonSerializer.Deserialize<RecordType>(ref reader)?.Type ??
                     
       // Throw Exception when json has no "Type" Property
       throw new RecordValidationException($"Error while extracting record type string. Does the JSON contain a {nameof(Record.Type)} field?");

    return _recordTypeCache.GetRecordType(typeString);
  }

  private Record Validate(Record record, Type type)
  {
    var missing = _recordTypeCache.GetNonNullableRecordProperties(type)
      .Where(property => property.GetValue(record) == null)
      .Select(property => property.Name)
      .ToList();
    
    if (missing.Count > 0)
      throw new RecordValidationException($"Error validating {type} with RecordId '{record.RecordId}'.\n" +
        $"One ore more non-nullable properties missing or null: {string.Join(", ", missing.Select(property => $"{type.Name}.{property}"))}");

    return record;
  }
}
