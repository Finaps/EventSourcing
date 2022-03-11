namespace EventSourcing.Core;

/// <summary>
/// Update <see cref="Projection"/>s in bulk, by rehydrating and persisting their source <see cref="Aggregate"/>s
/// </summary>
/// <remarks>
/// <para>
/// To update <see cref="Projection"/>s of a particular <see cref="Aggregate"/>,
/// rehydrate and persist this aggregate using the <see cref="IAggregateService"/>
/// </para>
/// <para>
/// The <see cref="Projection"/>.<see cref="Projection.Hash"/> property is used to decide whether it is out of date.
/// This hash is determined at projection creation time based on the
/// <see cref="Aggregate"/>.<see cref="Aggregate.ComputeHash"/> and <see cref="IProjectionFactory"/>.<see cref="IProjectionFactory"/>.<see cref="IProjectionFactory.ComputeHash"/> methods.
/// </para>
/// </remarks>
/// <seealso cref="IAggregateService"/>
/// <seealso cref="Aggregate"/>
/// <seealso cref="IProjectionFactory"/>
public class ProjectionUpdateService : IProjectionUpdateService
{
  private readonly IAggregateService _service;
  private readonly IRecordStore _store;

  /// <summary>
  /// Create new <see cref="ProjectionUpdateService"/>
  /// </summary>
  /// <param name="service"><see cref="IAggregateService"/></param>
  /// <param name="store"><see cref="IRecordStore"/></param>
  public ProjectionUpdateService(IAggregateService service, IRecordStore store)
  {
    _service = service;
    _store = store;
  }

  /// <summary>
  /// Update specified <see cref="Projection"/> type for a particular <see cref="Aggregate"/> type
  /// </summary>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  public async Task UpdateAllProjectionsAsync<TAggregate, TProjection>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new() where TProjection : Projection, new()
  {
    var factory = ProjectionCache.FactoryByAggregateAndProjection[(typeof(TAggregate), typeof(TProjection))];
    var hash = ProjectionCache.Hashes[factory.GetType().Name];

    // TODO: Make work with large data sets
    var items = await _store.GetProjections<TProjection>()
      .Where(x =>
        x.AggregateType == typeof(TAggregate).Name &&
        x.Hash != hash)
      .Select(x => new { x.PartitionId, x.AggregateId })
      .AsAsyncEnumerable()
      .ToListAsync(cancellationToken);

    foreach (var item in items)
    {
      var aggregate = await _service.RehydrateAsync<TAggregate>(item.PartitionId, item.AggregateId, cancellationToken);
      
      if (aggregate == null) continue;

      await _store.UpsertProjectionAsync(factory.CreateProjection(aggregate), cancellationToken);
    }
  }
}