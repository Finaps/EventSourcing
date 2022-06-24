using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace Finaps.EventSourcing.EF;

public class AggregateBuilder<TAggregate> where TAggregate : Aggregate, new()
{
  private static Dictionary<Type, List<Type>> Events = new();
  private static Dictionary<Type, List<Type>> Snapshots = new();
  private static Dictionary<Type, List<Type>> AggregateProjections = new();
  private static List<Type> Projections = new();

  static AggregateBuilder()
  {
    foreach (var type in EventSourcingCache.RecordTypes)
    {
      if (typeof(Event<>).IsAssignableFrom(type))
      {
        var aggregateType = GetFirstGenericArgument(type, typeof(Event<>));
        Events.TryAdd(aggregateType, new List<Type>());
        Events[aggregateType].Add(type);
      }
      else if (typeof(Snapshot<>).IsAssignableFrom(type))
      {
        var aggregateType = GetFirstGenericArgument(type, typeof(Snapshot<>));
        Snapshots.TryAdd(aggregateType, new List<Type>());
        Snapshots[aggregateType].Add(type);
      }
      else if (typeof(Projection).IsAssignableFrom(type))
      {
        Projections.Add(type);
      }
    }

    foreach (var factory in EventSourcingCache.GetProjectionFactories())
    {
      AggregateProjections.TryAdd(factory.AggregateType, new List<Type>());
      AggregateProjections[factory.AggregateType].Add(factory.ProjectionType);
    }
  }

  public void Build(ModelBuilder builder)
  {
    builder.EventCollection<TAggregate>();
    builder.SnapshotCollection<TAggregate>();
  }

  private static Type GetFirstGenericArgument(Type type, Type target)
  {
    Type? t;

    // Traverse down the inheritance chain until target is found (or we can't traverse further)
    for (t = type; t != target && t != null; t = t.BaseType) { }

    // When target is found, return first generic argument
    return t?.GetGenericArguments().FirstOrDefault() ??
           throw new ArgumentException($"Couldn't find generic aggregate type of {type}", nameof(type));
  }
}