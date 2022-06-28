using System.Reflection;

namespace Finaps.EventSourcing.Core;

/// <summary>
/// Create <c>TProjection</c> for <c>TAggregate</c>
/// </summary>
/// <typeparam name="TAggregate"><see cref="Aggregate{TAggregate}"/> type</typeparam>
/// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
public abstract class ProjectionFactory<TAggregate, TProjection> : IProjectionFactory where TAggregate : Aggregate where TProjection : Projection
{
  /// <inheritdoc />
  public Type AggregateType => typeof(TAggregate);

  /// <inheritdoc />
  public Type ProjectionType => typeof(TProjection);

  /// <inheritdoc />
  public Projection? CreateProjection(Aggregate aggregate)
  {
    var projection = CreateProjection((TAggregate)aggregate);

    if (projection == null) return null;
    
    return projection with
    {
      AggregateType = aggregate.Type,
      FactoryType = GetType().Name,

      PartitionId = aggregate.PartitionId,
      AggregateId = aggregate.Id,
      Version = aggregate.Version,

      Timestamp = DateTimeOffset.UtcNow,

      Hash = ProjectionCache.Hashes[GetType().Name]
    };
  }

  /// <summary>
  /// Create <c>TProjection</c> for <c>TAggregate</c>
  /// </summary>
  /// <param name="aggregate">Source <c>TAggregate</c></param>
  /// <returns>Resulting <c>TProjection</c> of <c>TAggregate</c></returns>
  protected abstract TProjection? CreateProjection(TAggregate aggregate);

  /// <summary>
  /// Compute hash for <c>TProjection</c>
  /// </summary>
  /// <remarks>
  /// By default, the IL bytecode of the
  /// <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply(Event{TAggregate})"/>,
  /// <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply(Snapshot{TAggregate})"/>
  /// and the <see cref="IProjectionFactory"/>.<see cref="IProjectionFactory.CreateProjection"/> methods,
  /// responsible for generating the T<see cref="Projection"/>, are used to create the hash.
  /// </remarks>
  /// <seealso cref="Projection"/>
  /// <seealso cref="Aggregate{TAggregate}"/>
  /// <seealso cref="IHashable"/>
  /// <returns>Hash string</returns>
  public virtual string ComputeHash() => IHashable.CombineHashes(
    IHashable.ComputeMethodHash(
      GetType().GetMethod(nameof(CreateProjection), BindingFlags.Instance | BindingFlags.NonPublic)),
      ProjectionCache.AggregateHashes[AggregateType]);
}