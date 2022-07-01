namespace Finaps.EventSourcing.Core;

/// <summary>
/// Cache for <see cref="Record"/>, <see cref="SnapshotFactory{TAggregate,TSnapshot}"/>, <see cref="ProjectionFactory{TAggregate,TProjection}"/>
/// </summary>
public static class Cache
{
  /// <summary>
  /// All <see cref="Record"/> types in the Assembly
  /// </summary>
  public static List<Type> RecordTypes { get; } = new();
  
  private static readonly Dictionary<Type, List<ISnapshotFactory>> SnapshotFactories = new();
  private static readonly Dictionary<Type, Dictionary<Type, IProjectionFactory>> ProjectionFactories = new();
  private static readonly Dictionary<string, string> ProjectionFactoryHashes = new();
  private static readonly Dictionary<Type, Type> ProjectionBaseTypes = new();

  static Cache()
  {
    var aggregateHashes = new Dictionary<Type, string>();
    
    foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x
               .GetTypes().Where(type => type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType)))
    {
      if (typeof(Record).IsAssignableFrom(type))
      {
        RecordTypes.Add(type);

        if (typeof(Projection).IsAssignableFrom(type))
          ProjectionBaseTypes[type] = GetBaseType(type);
      }
      else if (typeof(Aggregate).IsAssignableFrom(type))
      {
        var aggregate = (Aggregate)Activator.CreateInstance(type)!;
        aggregateHashes.Add(aggregate.GetType(), aggregate.ComputeHash());
      }
      else if (typeof(ISnapshotFactory).IsAssignableFrom(type))
      {
        var factory = (ISnapshotFactory)Activator.CreateInstance(type)!;
        SnapshotFactories.TryAdd(factory.AggregateType, new List<ISnapshotFactory>());
        SnapshotFactories[factory.AggregateType].Add(factory);
      }
      else if (typeof(IProjectionFactory).IsAssignableFrom(type))
      {
        var factory = (IProjectionFactory)Activator.CreateInstance(type)!;
        ProjectionFactories.TryAdd(factory.AggregateType, new Dictionary<Type, IProjectionFactory>());
        ProjectionFactories[factory.AggregateType][factory.ProjectionType] = factory;
      }
    }

    foreach (var factory in ProjectionFactories.Values.SelectMany(x => x.Values))
      ProjectionFactoryHashes[factory.AggregateType.Name] = IHashable.CombineHashes(
        factory.ComputeHash(), aggregateHashes[factory.AggregateType]);
  }

  /// <summary>
  /// Get <see cref="SnapshotFactory{TAggregate,TSnapshot}"/>s for <see cref="Aggregate"/> type
  /// </summary>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <returns><see cref="SnapshotFactory{TAggregate,TSnapshot}"/>s</returns>
  public static IEnumerable<ISnapshotFactory<TAggregate>> GetSnapshotFactories<TAggregate>() where TAggregate : Aggregate<TAggregate>, new() =>
    SnapshotFactories.TryGetValue(typeof(TAggregate), out var factories)
      ? factories.Cast<ISnapshotFactory<TAggregate>>()
      : Array.Empty<ISnapshotFactory<TAggregate>>();

  /// <summary>
  /// Get <see cref="ProjectionFactory{TAggregate,TProjection}"/> for <see cref="Aggregate"/> and <see cref="Projection"/>
  /// </summary>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <returns><see cref="ProjectionFactory{TAggregate,TProjection}"/> if defined</returns>
  public static IProjectionFactory? GetProjectionFactory<TAggregate, TProjection>()
    where TAggregate : Aggregate where TProjection : Projection => 
    ProjectionFactories.TryGetValue(typeof(TAggregate), out var factories)
      ? factories.TryGetValue(typeof(TProjection), out var factory) ? factory : null
      : null;

  /// <summary>
  /// Get <see cref="ProjectionFactory{TAggregate,TProjection}"/>s for <see cref="Aggregate"/> type
  /// </summary>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <returns><see cref="ProjectionFactory{TAggregate,TProjection}"/>s</returns>
  public static IEnumerable<IProjectionFactory> GetProjectionFactories<TAggregate>()
    where TAggregate : Aggregate<TAggregate>, new() => 
    ProjectionFactories.TryGetValue(typeof(TAggregate), out var factories)
      ? factories.Values
      : Array.Empty<IProjectionFactory>();

  /// <summary>
  /// Get Hash for <see cref="ProjectionFactory{TAggregate,TProjection}"/> type name
  /// </summary>
  /// <param name="projectionFactoryTypeName"><see cref="ProjectionFactory{TAggregate,TProjection}"/> type name</param>
  /// <returns>Hash if defined</returns>
  public static string? GetProjectionFactoryHash(string projectionFactoryTypeName) =>
    ProjectionFactoryHashes.TryGetValue(projectionFactoryTypeName, out var hash)
      ? hash
      : null;

  /// <summary>
  /// Get Base Type of <see cref="Projection"/> hierarchy
  /// </summary>
  /// <param name="projectionType"><see cref="Projection"/> type</param>
  /// <returns></returns>
  public static Type GetProjectionBaseType(Type projectionType) => ProjectionBaseTypes[projectionType];

  private static Type GetBaseType(Type type)
  {
    while (type.BaseType != typeof(Projection))
      type = type.BaseType!;
    return type;
  }
}