namespace EventSourcing.Core;

/// <summary>
/// Responsible for creating a <see cref="View"/> for every <see cref="IViewFactory"/> defined for a given <see cref="Aggregate"/>
/// </summary>
public static class ViewService
{
  private static readonly List<IViewFactory> ViewFactories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IViewFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (IViewFactory)Activator.CreateInstance(type)!)
    .ToList();

  private static readonly Dictionary<Type, List<IViewFactory>> AggregateViewFactories = ViewFactories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());
  
  /// <summary>
  /// Create all <see cref="View"/>s defined for a particular <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="View"/>s of <see cref="Aggregate"/></returns>
  public static List<View> CreateViews(Aggregate aggregate) => 
    AggregateViewFactories.TryGetValue(aggregate.GetType(), out var factories)
      ? factories.Select(x => x.CreateView(aggregate)).ToList()
      : new List<View>();
}