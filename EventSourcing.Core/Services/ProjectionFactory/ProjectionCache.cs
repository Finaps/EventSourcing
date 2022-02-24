namespace EventSourcing.Core;

public static class ProjectionCache
{
  public static readonly List<IProjectionFactory> Factories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IProjectionFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (IProjectionFactory) Activator.CreateInstance(type)!)
    .ToList();
  
  public static readonly Dictionary<Type, List<IProjectionFactory>> FactoriesByAggregate = Factories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());
  
  public static readonly Dictionary<(Type, Type), string> Hashes = Factories
    .ToDictionary(x => (x.AggregateType, x.ProjectionType), x => x.ComputeHash());
}