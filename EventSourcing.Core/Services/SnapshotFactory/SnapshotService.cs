namespace EventSourcing.Core;

/// <summary>
/// Create all <see cref="Snapshot"/>s for a particular <see cref="Aggregate{TAggregate}"/> type
/// </summary>
internal static class SnapshotService
{
  private static readonly List<ISnapshotFactory> SnapshotFactories = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(ISnapshotFactory).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .Select(type => (ISnapshotFactory)Activator.CreateInstance(type)!)
    .ToList();

  private static readonly Dictionary<Type, List<ISnapshotFactory>> AggregateSnapshotFactories = SnapshotFactories
    .GroupBy(x => x.AggregateType)
    .ToDictionary(x => x.Key, x => x.ToList());

  /// <summary>
  /// Create all <see cref="Snapshot"/>s for a particular <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregate"><see cref="Aggregate{TAggregate}"/> to create snapshot for</param>
  /// <returns></returns>
  public static List<Snapshot> CreateSnapshots(Aggregate aggregate) => 
    AggregateSnapshotFactories.TryGetValue(aggregate.GetType(), out var factories)
      ? factories
        .Where(x => x.IsSnapshotIntervalExceeded(aggregate))
        .Select(x => x.CreateSnapshot(aggregate))
        .ToList()
      : new List<Snapshot>();
}