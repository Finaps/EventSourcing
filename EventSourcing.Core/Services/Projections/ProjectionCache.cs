namespace EventSourcing.Core;

public static class ProjectionCache
{
  public static readonly Dictionary<Type, string> AggregateHashes = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(Aggregate).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .ToDictionary(type => type, type => ((Aggregate)Activator.CreateInstance(type)!).ComputeHash());

  public static readonly List<IProjectionFactory> Factories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IProjectionFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (IProjectionFactory) Activator.CreateInstance(type)!)
    .ToList();
  
  public static readonly Dictionary<Type, List<IProjectionFactory>> FactoriesByAggregate = Factories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());

  public static readonly Dictionary<string, string> Hashes = Factories
    .Select(x => new IHashable[] { x, (IHashable)Activator.CreateInstance(x.AggregateType) })
    .ToDictionary(x => x.First().GetType().Name, x => IHashable.CombineHashes(x.Select(y => y.ComputeHash()).ToArray()));
}