namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="Projection"/> for <see cref="Aggregate"/>
/// </summary>
public interface IProjectionFactory
{
  Type AggregateType { get; }
  Type ProjectionType { get; }
  
  /// <summary>
  /// Create <see cref="Projection"/> for <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="Projection"/> of <see cref="Aggregate"/></returns>
  Projection CreateProjection(Aggregate aggregate);
}