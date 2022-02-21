namespace EventSourcing.Core;

public static class SnapshotService
{
  private static readonly List<ISnapshotFactory> ViewFactories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(ISnapshotFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (ISnapshotFactory)Activator.CreateInstance(type)!)
    .ToList();

  private static readonly Dictionary<Type, List<ISnapshotFactory>> AggregateViewFactories = ViewFactories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());

  public static List<Snapshot> CreateSnapshots(Aggregate aggregate) => 
    AggregateViewFactories.TryGetValue(aggregate.GetType(), out var factories)
      ? factories
        .Where(x => x.IsSnapshotIntervalExceeded(aggregate))
        .Select(x => x.CreateSnapshot(aggregate))
        .ToList()
      : new List<Snapshot>();
}