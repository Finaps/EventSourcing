namespace Finaps.EventSourcing.Core;

/// <inheritdoc />
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
  /// Update specified <see cref="Projection"/> type for a particular <see cref="Aggregate{TAggregate}"/> type
  /// </summary>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate{TAggregate}"/> type</typeparam>
  /// <typeparam name="TProjection"><see cref="Projection"/> type</typeparam>
  public async Task UpdateAllProjectionsAsync<TAggregate, TProjection>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new() where TProjection : Projection
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