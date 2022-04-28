namespace Finaps.EventSourcing.Core;

internal static class ProjectionCache
{
  private static readonly List<Aggregate> Aggregates = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(Aggregate).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (Aggregate) Activator.CreateInstance(type)!)
    .ToList();
    
  private static readonly List<IProjectionFactory> Factories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IProjectionFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (IProjectionFactory) Activator.CreateInstance(type)!)
    .ToList();

  public static readonly Dictionary<Type, string> AggregateHashes = Aggregates
    .ToDictionary(x => x.GetType(), x => x.ComputeHash());

  public static readonly Dictionary<Type, List<IProjectionFactory>> FactoriesByAggregate = Factories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());

  public static readonly Dictionary<(Type, Type), IProjectionFactory> FactoryByAggregateAndProjection = Factories
    .GroupBy(x => (x.AggregateType, x.ProjectionType))
    .ToDictionary(x => x.Key, x => x.First());

  public static readonly Dictionary<string, string> Hashes = Factories
    .Select(x => new IHashable[] { x, (IHashable)Activator.CreateInstance(x.AggregateType)! })
    .ToDictionary(x => x.First().GetType().Name, x => IHashable.CombineHashes(x.Select(y => y.ComputeHash()).ToArray()));
}