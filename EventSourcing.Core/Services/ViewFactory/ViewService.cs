namespace EventSourcing.Core;

/// <summary>
/// Responsible for creating a <see cref="Projection"/> for every <see cref="IProjectionFactory"/> defined for a given <see cref="Aggregate"/>
/// </summary>
public static class ProjectionService
{
  private static readonly List<IProjectionFactory> ProjectionFactories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IProjectionFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (IProjectionFactory)Activator.CreateInstance(type)!)
    .ToList();

  private static readonly Dictionary<Type, List<IProjectionFactory>> AggregateProjectionFactories = ProjectionFactories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());
  
  /// <summary>
  /// Create all <see cref="Projection"/>s defined for a particular <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="Projection"/>s of <see cref="Aggregate"/></returns>
  public static List<Projection> CreateProjections(Aggregate aggregate) => 
    AggregateProjectionFactories.TryGetValue(aggregate.GetType(), out var factories)
      ? factories.Select(x => x.CreateProjection(aggregate)).ToList()
      : new List<Projection>();
}