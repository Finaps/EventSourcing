namespace EventSourcing.Core;

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
    where TAggregate : Aggregate, new() where TProjection : Projection, new();
}