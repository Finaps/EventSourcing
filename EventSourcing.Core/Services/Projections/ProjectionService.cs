namespace EventSourcing.Core;

/// <summary>
/// Responsible for creating a <see cref="Projection"/> for every <see cref="IProjectionFactory"/> defined for a given <see cref="Aggregate"/>
/// </summary>
public static class ProjectionService
{
  /// <summary>
  /// Create all <see cref="Projection"/>s defined for a particular <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="Projection"/>s of <see cref="Aggregate"/></returns>
  public static List<Projection> CreateProjections(Aggregate aggregate) => 
    ProjectionCache.FactoriesByAggregate.TryGetValue(aggregate.GetType(), out var factories)
      ? factories.Select(x => x.CreateProjection(aggregate)).ToList()
      : new List<Projection>();
}