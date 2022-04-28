using System.Reflection;

namespace Finaps.EventSourcing.Core;

/// <summary>
/// Create <see cref="TProjection"/> for <see cref="TAggregate"/>
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
  public Projection CreateProjection(Aggregate aggregate) => CreateProjection((TAggregate) aggregate) with
  {
    AggregateType = aggregate.Type,
    FactoryType = GetType().Name,
    
    PartitionId = aggregate.PartitionId,
    AggregateId = aggregate.Id,
    Version = aggregate.Version,
    
    Timestamp = DateTimeOffset.Now,
    
    Hash = ProjectionCache.Hashes[GetType().Name]
  };
  
  /// <summary>
  /// Create <see cref="TProjection"/> for <see cref="TAggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="TAggregate"/></param>
  /// <returns>Resulting <see cref="TProjection"/> of <see cref="TAggregate"/></returns>
  protected abstract TProjection CreateProjection(TAggregate aggregate);

  /// <summary>
  /// Compute hash for <see cref="TProjection"/>
  /// </summary>
  /// <remarks>
  /// By default, the IL bytecode of the T<see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Apply"/>
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