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
  /// Update all <see cref="Projection"/>s for a particular <see cref="Aggregate"/> type
  /// </summary>
  /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
  public async Task UpdateAllProjectionsAsync<TAggregate>(CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    // Get Projection Factories for TAggregate
    var factories = ProjectionCache.FactoriesByAggregate[typeof(TAggregate)]
      .ToDictionary(x => x.ProjectionType.Name, x => x);
    
    // Het hashes for each of those factories, representing the logic of the aggregate and projection factory
    var hashes = factories.Values.Select(x => ProjectionCache.Hashes[x.GetType().Name]);
    
    var items = _store.Projections

      // Get Outdated Projections, i.e. those whose stored hash differs from the local hash
      // The Contains method assumes there are no hash collisions for the projections of this aggregate type
      .Where(x => x.AggregateType == typeof(TAggregate).Name && !hashes.Contains(x.Hash))
      
      // Cosmos Linq does not support the GroupBy method. Hence we're emulating it using OrderBy
      .OrderBy(x => x.AggregateId)
      
      // We need PartitionId and AggregateId for rehydration and Projection Type for the ProjectionFactory
      .Select(x => new { x.PartitionId, x.AggregateId, x.Type })
      
      .AsAsyncEnumerable()
      .WithCancellation(cancellationToken);

    Aggregate? aggregate = null;
    IRecordTransaction? transaction = null;

    await foreach (var item in items)
    {
      // Prevent Hydrating Aggregates more times than necessary, ties into the OrderBy method of the items query
      if (aggregate == null || aggregate.Id != item.AggregateId)
      {
        // Commit the previous transaction
        if (transaction != null) await transaction.CommitAsync(cancellationToken);

        aggregate = await _service.RehydrateAsync<TAggregate>(item.PartitionId, item.AggregateId, cancellationToken);
        transaction = _store.CreateTransaction(item.PartitionId);
      }
      
      // If Events are deleted, but Projections remain, it could be that RehydrateAsync returns null
      if (aggregate != null && transaction != null)
        transaction.UpsertProjection(factories[item.Type].CreateProjection(aggregate));
    }

    // Commit the last transaction
    if (transaction != null) await transaction.CommitAsync(cancellationToken);
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

    var items = _store.Projections
      .Where(x =>
        x.AggregateType == typeof(TAggregate).Name &&
        x.Type == typeof(TProjection).Name &&
        x.Hash != hash)
      .Select(x => new { x.PartitionId, x.AggregateId })
      .AsAsyncEnumerable()
      .WithCancellation(cancellationToken);

    await foreach (var item in items)
    {
      var aggregate = await _service.RehydrateAsync<TAggregate>(item.PartitionId, item.AggregateId, cancellationToken);
      
      if (aggregate == null) continue;

      await _store.UpsertProjectionAsync(factory.CreateProjection(aggregate), cancellationToken);
    }
  }
}