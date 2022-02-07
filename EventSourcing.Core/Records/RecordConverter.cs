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
    .Where(type => typeof(Record).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
    .ToList();

  private static readonly List<Type> AssemblyMigratorTypes = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IRecordMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
    .ToList();

  private readonly Dictionary<string, Type> _recordTypes;
  private readonly Dictionary<Type, PropertyInfo[]> _nonNullableRecordProperties;

  private readonly Dictionary<string, IRecordMigrator> _migrators;

  private class RecordType
  {
    public string Type { get; set; }
  }

  public RecordConverter(RecordConverterOptions? options = null)
  {
    // Create dictionary mapping from Record.Type string to Record Type
    _recordTypes = (options?.RecordTypes ?? AssemblyRecordTypes).ToDictionary(type => type.Name);
    
    // For each Record Type, create set of non-nullable properties for validation
    _nonNullableRecordProperties = _recordTypes.Values.ToDictionary(type => type, type => type.GetProperties()
      .Where(property => Nullable.GetUnderlyingType(property.PropertyType) == null).ToArray());
    
    // Create dictionary mapping from Record.Type to Migrator Type
    _migrators = (options?.MigratorTypes ?? AssemblyMigratorTypes)
      .Select(type => Activator.CreateInstance(type) as IRecordMigrator)
      .ToDictionary(migrator => migrator!.Source.Name, migrator => migrator);

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
       throw new RecordValidationException($"Error while extracting record type string. Does the JSON contain a {nameof(Record.Type)} field?");

    return GetRecordType(typeString);
  }

  private Type GetRecordType(string typeString)
  {
    // Get actual Record Type from Dictionary
    if (!_recordTypes.TryGetValue(typeString, out var type))
      
      // Throw Exception when Record Type is not found in Assembly or RecordConverterOptions
      throw new InvalidOperationException($"Record with Type '{typeString}' not found");

    return type;
  }

  private TRecord Validate(TRecord record, Type type)
  {
    var missing = _nonNullableRecordProperties[type]
      .Where(property => property.GetValue(record) == null)
      .Select(property => property.Name)
      .ToList();
    
    if (missing.Count > 0)
      throw new RecordValidationException($"Error validating {type} with RecordId '{record.RecordId}'.\n" +
        $"One ore more non-nullable properties missing or null: {string.Join(", ", missing.Select(property => $"{type.Name}.{property}"))}");

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
    var unvisitedMigrators = _migrators.Values.ToDictionary(x => x.GetType(), x => x);
    
    while (unvisitedMigrators.Count > 0)
    {
      // Pop item from unvisited Migrators
      var source = unvisitedMigrators.First().Value;
      unvisitedMigrators.Remove(source.GetType());
      
      var visited = new List<Type> { source.GetType() };

      while (unvisitedMigrators.TryGetValue(source.Target, out var target))
      {
        if (visited.Contains(target.GetType()))
          throw new ArgumentException("Record Migrator Collection contains cyclic references: " +
            string.Join(" -> ", visited.SkipWhile(type => type != source.Target).Append(source.Target)));
        
        unvisitedMigrators.Remove(target.GetType());
        visited.Add(target.GetType());

        source = target;
      }
    }
  }
}
