using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public class RecordContext : DbContext
{
  public RecordContext() {}
  public RecordContext(DbContextOptions options) : base(options) {}
  protected override void OnModelCreating(ModelBuilder builder)
  {
    var records = GetAssemblyTypes<Record>();
    
    var events = records
      .Where(type => typeof(Event).IsAssignableFrom(type))
      .GroupBy(GetAggregateType).Where(x => x.Key != null)
      .ToDictionary(grouping => grouping.Key!, grouping => grouping.ToList());
    
    var snapshots = records
      .Where(type => typeof(Snapshot).IsAssignableFrom(type))
      .GroupBy(GetAggregateType).Where(x => x.Key != null)
      .ToDictionary(grouping => grouping.Key!, grouping => grouping.ToList());

    var projections = records
      .Where(type => typeof(Projection).IsAssignableFrom(type))
      .ToList();

    foreach (var (aggregateType, types) in events)
    {
      builder.EventEntity(aggregateType);
      builder.SnapshotEntity(aggregateType);
      foreach (var type in types) builder.Entity(type).HasDiscriminator<string>(nameof(Event.Type));
    }
    
    foreach (var type in snapshots.Values.SelectMany(types => types))
      builder.Entity(type).HasDiscriminator<string>(nameof(Event.Type));

    foreach (var projection in projections)
      builder.ProjectionEntity(projection);
  }

  private static Type? GetAggregateType(Type? type)
  {
    while (type != null)
    {
      var aggregateType = type.GetGenericArguments().FirstOrDefault(typeof(Aggregate).IsAssignableFrom);
      if (aggregateType != null) return aggregateType;
      type = type.BaseType;
    }

    return null;
  }

  private static List<Type> GetAssemblyTypes<T>() => AppDomain.CurrentDomain
    .GetAssemblies().SelectMany(x => x.GetTypes())
    .Where(type => typeof(T).IsAssignableFrom(type) && type.IsPublic && type.IsClass && !type.IsAbstract &&
                   !type.IsGenericType &&
                   type.GetConstructor(Type.EmptyTypes) != null)
    .ToList();
}
