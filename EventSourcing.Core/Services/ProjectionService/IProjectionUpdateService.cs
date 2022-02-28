namespace EventSourcing.Core;

public interface IProjectionUpdateService
{
  /// <summary>
  /// Update all outdated <see cref="Projection"/>s for a given <see cref="Aggregate"/> type
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <param name="aggregateType"><see cref="Aggregate"/> type</param>
  /// <param name="projectionType"><see cref="Projection"/> type</param>
  /// <param name="cancellationToken">Cancellation token</param>
  Task UpdateProjectionsAsync(Type aggregateType, Type projectionType, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Update all outdated <see cref="Projection"/>s for a given <see cref="Aggregate"/> type
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <param name="aggregateType"><see cref="Aggregate"/> type</param>
  /// <param name="cancellationToken">Cancellation token</param>
  Task UpdateProjectionsAsync(Type aggregateType, CancellationToken cancellationToken = default);
  
  /// <summary>
  /// Update all outdated <see cref="Projection"/>s
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <param name="cancellationToken">Cancellation token</param>
  Task UpdateProjectionsAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Update all outdated <see cref="Projection"/>s for a given <see cref="Aggregate"/> type
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <param name="cancellationToken">Cancellation token</param>
  async Task UpdateProjectionsAsync<TAggregate>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate =>
    await UpdateProjectionsAsync(typeof(TAggregate), cancellationToken);

  /// <summary>
  /// Update all outdated <see cref="Projection"/>s for a given <see cref="Aggregate"/> type
  /// </summary>
  /// <remarks>The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to determine if a projection is out of date</remarks>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  /// <param name="cancellationToken">Cancellation token</param>
  async Task UpdateProjectionsAsync<TAggregate, TProjection>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate where TProjection : Projection => 
    await UpdateProjectionsAsync(typeof(TAggregate), cancellationToken);
}