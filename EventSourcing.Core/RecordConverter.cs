using EventSourcing.Core.Migrations;

namespace EventSourcing.Core;

/// <summary>
/// Custom <see cref="Record"/><see cref="JsonConverter{T}"/>
/// </summary>
/// <remarks>
/// Enables Polymorphic Serialization and Deserialization using the <see cref="Record"/>.<see cref="Record.Type"/> property
/// </remarks>
public class RecordConverter<TRecord> : JsonConverter<TRecord> where TRecord : Record
{
  public RecordConverter() =>  ValidateMigrators();

  private class RecordType
  {
    public string Type { get; set; }
  }

  /// <summary>
  /// Dictionary containing mapping between <see cref="Record"/>.<see cref="Record.Type"/> string and actual <see cref="Record"/> type
  /// </summary>
  private static readonly Dictionary<string, Type> RecordTypes =
    AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
      .ToDictionary(type => type.Name);
  
  /// <summary>
  /// Dictionary containing mapping between <see cref="Event"/>.<see cref="Event.Type"/> string and their <see cref="IRecordMigrator"/>
  /// </summary>
  private static readonly Dictionary<string, IRecordMigrator> Migrators =
    AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => typeof(IRecordMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
      .Select(type => Activator.CreateInstance(type) as IRecordMigrator)
      .ToDictionary(migrator => migrator!.Source.Name, migrator => migrator);

  /// <summary>
  /// Use <see cref="RecordConverter{TRecord}"/> for all Types inheriting from <see cref="Record"/>
  /// </summary>
  /// <param name="typeToConvert">Type to Convert</param>
  public override bool CanConvert(Type typeToConvert) =>
    typeof(Record).IsAssignableFrom(typeToConvert);

  /// <summary>
  /// Serialize Record
  /// </summary>
  public override void Write(Utf8JsonWriter writer, TRecord value, JsonSerializerOptions options) =>
    JsonSerializer.Serialize(writer, value, RecordTypes[value.Type]);

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var readerClone = reader;
    var typeString = JsonSerializer.Deserialize<RecordType>(ref readerClone)?.Type
        ?? throw new JsonException($"Error while extracting record type string. Does the JSON contain a {nameof(Record.Type)} field?");
    var type = RecordTypes[typeString];

    return Migrate((TRecord) JsonSerializer.Deserialize(ref reader, type));
  }
  
  private TRecord Migrate(TRecord record)
  {
    while (Migrators.TryGetValue(record.Type, out var migrator))
      record = (TRecord) migrator.Convert(record);

    return record;
  }
  
  private static void ValidateMigrators()
  {
    foreach (var (source, m) in Migrators)
    {
      var migrator = m;
      
      while (Migrators.TryGetValue(migrator.Target.Name, out migrator))
        if (migrator.Target.Name == source)
          throw new InvalidOperationException($"Loop detected in event migrators containing {source}");
    }
  }
}
