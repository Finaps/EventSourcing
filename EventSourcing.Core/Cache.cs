namespace Finaps.EventSourcing.Core;

public static class Cache
{
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
  
  public static IEnumerable<ISnapshotFactory> GetSnapshotFactories(Type aggregateType) =>
    SnapshotFactories.TryGetValue(aggregateType, out var factories)
      ? factories
      : Array.Empty<ISnapshotFactory>();

  public static IProjectionFactory? GetProjectionFactory<TAggregate, TProjection>()
    where TAggregate : Aggregate where TProjection : Projection => 
    ProjectionFactories.TryGetValue(typeof(TAggregate), out var factories)
      ? factories.TryGetValue(typeof(TProjection), out var factory) ? factory : null
      : null;

  public static IEnumerable<IProjectionFactory> GetProjectionFactories(Type aggregateType) =>
    ProjectionFactories.TryGetValue(aggregateType, out var factories)
      ? factories.Values
      : Array.Empty<IProjectionFactory>();

  public static IEnumerable<IProjectionFactory> GetProjectionFactories() =>
    ProjectionFactories.Values.SelectMany(x => x.Values);

  public static string? GetProjectionFactoryHash(string projectionFactoryTypeName) =>
    ProjectionFactoryHashes.TryGetValue(projectionFactoryTypeName, out var hash)
      ? hash
      : null;

  public static Type GetProjectionBaseType(Type type) => ProjectionBaseTypes[type];

  private static Type GetBaseType(Type type)
  {
    while (type.BaseType != typeof(Projection))
      type = type.BaseType!;
    return type;
  }
}