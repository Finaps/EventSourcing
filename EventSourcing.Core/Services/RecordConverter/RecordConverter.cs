namespace EventSourcing.Core;

/// <summary>
/// Custom <see cref="Record"/><see cref="JsonConverter{T}"/>
/// </summary>
/// <remarks>
/// Enables Polymorphic Serialization and Deserialization using the <see cref="Record"/>.<see cref="Record.Type"/> property
/// </remarks>
public class RecordConverter<TRecord> : JsonConverter<TRecord> where TRecord : Record
{
  private class RecordType
  {
    public string? Type { get; set; }
  }

  private readonly RecordTypeCache _recordTypeCache;
  private readonly EventMigratorService _eventMigratorService;

  public RecordConverter(RecordConverterOptions? options = null)
  {
    _recordTypeCache = new RecordTypeCache(options?.RecordTypes);
    _eventMigratorService = new EventMigratorService(options?.MigratorTypes);
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
    var type = value.GetType();
    var record = Validate(value, type);
    JsonSerializer.Serialize(writer, record, type);
  }

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var type = DeserializeRecordType(reader);
    var record = JsonSerializer.Deserialize(ref reader, type) as Record
                 ?? throw new JsonException($"Error Converting Json to {type.Name}.");

    return (TRecord)(
      record is Event
        ? _eventMigratorService.Migrate((Event)Validate(record, type))
        : record
    );
  }

  private Type DeserializeRecordType(Utf8JsonReader reader)
  {
    // Get Record.Type String from Json
    var typeString = JsonSerializer.Deserialize<RecordType>(ref reader)?.Type ??

                     // Throw Exception when json has no "Type" Property
                     throw new RecordValidationException(
                       $"Error converting {typeof(TRecord)}. " +
                       $"Couldn't parse {typeof(TRecord)}.Type string from Json. " +
                       $"Does the Json contain a {nameof(Record.Type)} field?");

    return _recordTypeCache.GetRecordType(typeString);
  }

  private Record Validate(Record record, Type type)
  {
    var missing = _recordTypeCache.GetNonNullableRecordProperties(type)
      .Where(property => property.GetValue(record) == null)
      .Select(property => property.Name)
      .ToList();

    if (missing.Count > 0)
      throw new RecordValidationException(
        $"Error converting Json to {record}'.\n" +
        $"One ore more non-nullable properties are missing or null: {string.Join(", ", missing.Select(property => $"{type.Name}.{property}"))}.\n" +
        $"Either make properties nullable or use a RecordMigrator to handle {nameof(TRecord)} versioning.");

    return record;
  }
}