using EventSourcing.Core.Migrations;

namespace EventSourcing.Core;

/// <summary>
/// Custom <see cref="Event"/><see cref="JsonConverter{T}"/>
/// </summary>
/// <remarks>
/// Enables Polymorphic Serialization and Deserialization using the <see cref="Event"/>.<see cref="Event.Type"/> property
/// </remarks>
public class EventConverter<TEvent> : JsonConverter<TEvent> where TEvent : Event
{
  public EventConverter()
  {
    ValidateMigrators();
  }
  private class EventType
  {
    public string Type { get; set; }
  }
  
  /// <summary>
  /// Dictionary containing mapping between <see cref="Event"/>.<see cref="Event.Type"/> string and actual <see cref="Event"/> type
  /// </summary>
  private static readonly Dictionary<string, Type> EventTypes =
    AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => typeof(Event).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
      .ToDictionary(type => type.Name);
  
  /// <summary>
  /// Dictionary containing mapping between <see cref="Event"/>.<see cref="Event.Type"/> string and their <see cref="IEventMigrator"/>
  /// </summary>
  private static readonly Dictionary<string, IEventMigrator> Migrators =
    AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(assembly => assembly.GetTypes())
      .Where(type => typeof(IEventMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
      .Select(type => Activator.CreateInstance(type) as IEventMigrator)
      .ToDictionary(migrator => migrator!.Source.Name, migrator => migrator);
  
  /// <summary>
  /// Use <see cref="EventConverter"/> for all Types inheriting from <see cref="Event"/>
  /// </summary>
  /// <param name="typeToConvert">Type to Convert</param>
  public override bool CanConvert(Type typeToConvert) =>
    typeof(Event).IsAssignableFrom(typeToConvert);

  /// <summary>
  /// Serialize Event
  /// </summary>
  public override void Write(Utf8JsonWriter writer, TEvent value, JsonSerializerOptions options) =>
    JsonSerializer.Serialize(writer, value, EventTypes[value.Type]);

  /// <summary>
  /// Deserialize Event
  /// </summary>
  /// <exception cref="JsonException">Thrown when <see cref="Event"/> type cannot be found.</exception>
  public override TEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var readerClone = reader;
    var typeString = JsonSerializer.Deserialize<EventType>(ref readerClone)?.Type
      ?? throw new JsonException($"Error while extracting event type string. Does the JSON contain a {nameof(EventType.Type)} field?");
    var type = EventTypes[typeString];
      
    var e = (TEvent) JsonSerializer.Deserialize(ref reader, type);
    return Migrate(e);
  }
  
  private TEvent Migrate(TEvent e)
  {
    while (Migrators.TryGetValue(e.GetType().Name, out var migrator))
      e = (TEvent) migrator.Convert(e);

    return e;
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
