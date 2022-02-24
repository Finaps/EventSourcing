namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="TProjection"/> for <see cref="TAggregate"/>
/// </summary>
/// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
/// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
public abstract class ProjectionFactory<TAggregate, TProjection> : IProjectionFactory where TAggregate : Aggregate where TProjection : Projection
{
  public Type AggregateType => typeof(TAggregate);
  public Type ProjectionType => typeof(TProjection);
  
  public Projection CreateProjection(Aggregate aggregate) => CreateProjection((TAggregate) aggregate) with
  {
    AggregateType = aggregate.Type,
    PartitionId = aggregate.PartitionId,
    AggregateId = aggregate.Id,
    Version = aggregate.Version
  };
  
  /// <summary>
  /// Create <see cref="TProjection"/> for <see cref="TAggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="TAggregate"/></param>
  /// <returns>Resulting <see cref="TProjection"/> of <see cref="TAggregate"/></returns>
  protected abstract TProjection CreateProjection(TAggregate aggregate);
}