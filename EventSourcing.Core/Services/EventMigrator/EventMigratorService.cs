namespace EventSourcing.Core;

/// <summary>
/// Responsible for migrating <see cref="Event"/>s after they are deserialized.
/// </summary>
public class EventMigratorService
{
  private static List<Type> AssemblyMigratorTypes => AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IEventMigrator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .ToList();

  private readonly Dictionary<Type, IEventMigrator?> _migrators;

  /// <summary>
  /// Create new <see cref="EventMigratorService"/>
  /// </summary>
  /// <param name="migratorTypes">
  /// Optional <see cref="List{T}"/> of <see cref="IEventMigrator"/> types.
  /// If not specified will default to all <see cref="IEventMigrator"/>s in assembly.
  /// </param>
  public EventMigratorService(List<Type>? migratorTypes = null)
  {
    // Create dictionary mapping from Record.Type to Migrator Type
    _migrators = (migratorTypes ?? AssemblyMigratorTypes)
      .Select(type => Activator.CreateInstance(type) as IEventMigrator)
      .ToDictionary(migrator => migrator!.Source, migrator => migrator);

    ValidateMigrators();
  }

  /// <summary>
  /// Migrate an <see cref="Event"/> to a newer schema version
  /// </summary>
  /// <param name="e"><see cref="Event"/> to migrate</param>
  /// <returns>Migrated <see cref="Event"/></returns>
  public Event Migrate(Event e)
  {
    while (_migrators.TryGetValue(e.GetType(), out var migrator))
      e = migrator!.Migrate(e);

    return e;
  }

  /// <summary>
  /// Checks if there are cyclic references (e.g. infinite loops) in the <see cref="IEventMigrator"/> collection.
  /// </summary>
  private void ValidateMigrators()
  {
    var migrations = _migrators.Values.ToDictionary(x => x!.Source, x => x!.Target);

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