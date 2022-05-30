namespace Finaps.EventSourcing.Core;

/// <summary>
/// Update <see cref="Projection"/>s in bulk, by rehydrating and persisting their source <see cref="Aggregate{TAggregate}"/>s
/// </summary>
/// <remarks>
/// <para>
/// To update <see cref="Projection"/>s of a particular <see cref="Aggregate{TAggregate}"/>,
/// rehydrate and persist this aggregate using the <see cref="IAggregateService"/>
/// </para>
/// <para>
/// The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to decide whether it is out of date.
/// This hash is determined at projection creation time based on the
/// <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.ComputeHash"/> and <see cref="IProjectionFactory"/>.<see cref="IProjectionFactory"/>.<see cref="IProjectionFactory.ComputeHash"/> methods.
/// </para>
/// </remarks>
/// <seealso cref="IAggregateService"/>
/// <seealso cref="Aggregate{TAggregate}"/>
/// <seealso cref="IProjectionFactory"/>
public interface IProjectionUpdateService
{
  /// <summary>
  /// Update all outdated <see cref="Projection"/>s for a given <see cref="Aggregate{TAggregate}"/> type
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <typeparam name="TAggregate"><see cref="Aggregate{TAggregate}"/> type</typeparam>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <param name="cancellationToken">Cancellation token</param>
  Task UpdateAllProjectionsAsync<TAggregate, TProjection>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new() where TProjection : Projection;
}