using System.Reflection;
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
  private static readonly List<Type> AssemblyRecordTypes = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(x => x.GetTypes())
    .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .ToList();

  private static readonly List<Type> AssemblyMigratorTypes = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IRecordMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .ToList();

  private readonly RecordConverterOptions? _options;
  private readonly Dictionary<string, Type> _recordTypes;
  private readonly Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;

  private readonly Dictionary<string, IRecordMigrator> _migrators;

  private class RecordType
  {
    public string Type { get; set; }
  }

  public RecordConverter(RecordConverterOptions? options = null)
  {
    _options = options;
    
    // Create dictionary mapping from Record.Type string to Record Type
    _recordTypes = (options?.RecordTypes ?? AssemblyRecordTypes).ToDictionary(type => type.Name);
    
    // For each Record Type, create set of non-nullable properties for validation
    _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
      .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    
    // Create dictionary mapping from Record.Type to Migrator Type
    _migrators = (options?.MigratorTypes ?? AssemblyMigratorTypes)
      .Select(type => Activator.CreateInstance(type) as IRecordMigrator)
      .ToDictionary(migrator => migrator!.Source.Name, migrator => migrator)!;

    ValidateMigrators();
  }

  /// <summary>
  /// Use <see cref="RecordConverter{TRecord}"/> for all Types inheriting from <see cref="Record"/>
  /// </summary>
  /// <param name="typeToConvert">Type to Convert</param>
  public override bool CanConvert(Type typeToConvert) => typeof(Record).IsAssignableFrom(typeToConvert);

  /// <summary>
  /// Serialize Record
  /// </summary>
  public override void Write(Utf8JsonWriter writer, TRecord value, JsonSerializerOptions options)
  {
    var type = GetRecordType(value.Type);
    var record = Validate(value, type);
    JsonSerializer.Serialize(writer, record, type);
  }

  /// <summary>
  /// Deserialize Record
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Record"/> type cannot be found.</exception>
  public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var type = GetRecordType(reader);
    var record = JsonSerializer.Deserialize(ref reader, type) as TRecord;
    return Migrate(Validate(record, type));
  }

  private Type GetRecordType(Utf8JsonReader reader)
  {
    // Get Record.Type String from Json
    var typeString = JsonSerializer.Deserialize<RecordType>(ref reader)?.Type ??
                     
       // Throw Exception when json has no "Type" Property
       throw new RecordValidationException(
         $"Error converting {typeof(TRecord)}. " +
         $"Couldn't parse {typeof(TRecord)}.Type string from Json. " +
         $"Does the Json contain a {nameof(Record.Type)} field?");

    return GetRecordType(typeString);
  }

  private Type GetRecordType(string typeString)
  {
    // Get Record Type from Dictionary
    if (_recordTypes.TryGetValue(typeString, out var type)) return type;

    if (_options?.MigratorTypes == null)
      throw new InvalidOperationException(
        $"Error Converting {typeof(TRecord)}. {typeof(TRecord)} with type '{typeString}' not found in Assembly. Ensure {typeof(TRecord)} with type '{typeString}' is a public non-nested type");
    else
      throw new InvalidOperationException(
        $"Error Converting {typeof(TRecord)}. {typeof(TRecord)} with type '{typeString}' not found in {nameof(RecordConverterOptions)}.{nameof(RecordConverterOptions.RecordTypes)}. Ensure {typeof(TRecord)} with type '{typeString}' is included.");
  }

  private TRecord Validate(TRecord record, Type type)
  {
    var missing = _nonNullableRecordProperties[type]
      .Where(property => property.GetValue(record) == null)
      .Select(property => property.Name)
      .ToList();
    
    if (missing.Count > 0)
      throw new RecordValidationException(
        $"Error converting Json to {record.Format()}'.\n" +
        $"One ore more non-nullable properties are missing or null: {string.Join(", ", missing.Select(property => $"{type.Name}.{property}"))}.\n" +
        $"Either make properties nullable or use a RecordMigrator to handle {nameof(TRecord)} versioning.");

    return record;
  }

  private TRecord Migrate(TRecord record)
  {
    while (_migrators.TryGetValue(record.Type, out var migrator))
      record = (TRecord) migrator.Convert(record);

    return record;
  }
  
  private void ValidateMigrators()
  {
    var migrations = _migrators.Values.ToDictionary(x => x.Source, x => x.Target);

    while (migrations.Count > 0)
    {
      var source = migrations.First().Key;
      var visited = new List<Type> { source };
      
      while (migrations.TryGetValue(source, out var target))
      {
        visited.Add(source);
        migrations.Remove(source);
        
        if (visited.Contains(target))
          throw new ArgumentException("Record Migrator Collection contains cyclic reference(s)");
        
        source = target;
      }
    }
  }
}
