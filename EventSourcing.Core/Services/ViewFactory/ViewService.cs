namespace EventSourcing.Core;

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

  public static List<View> GetViews(Aggregate aggregate) => 
    AggregateViewFactories.TryGetValue(aggregate.GetType(), out var factories)
      ? factories.Select(x => x.CreateView(aggregate)).ToList()
      : new List<View>();
}