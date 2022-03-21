namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="Projection"/> for <see cref="Aggregate{TAggregate}"/>
/// </summary>
public interface IProjectionFactory : IHashable
{
  /// <summary>
  /// Source <see cref="Aggregate{TAggregate}"/> type
  /// </summary>
  Type AggregateType { get; }
  
  /// <summary>
  /// Destination <see cref="Projection"/> type
  /// </summary>
  Type ProjectionType { get; }
  
  /// <summary>
  /// Create <see cref="Projection"/> for <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate{TAggregate}"/></param>
  /// <returns>Resulting <see cref="Projection"/> of <see cref="Aggregate{TAggregate}"/></returns>
  Projection CreateProjection(Aggregate aggregate);
}