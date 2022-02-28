using System.Reflection;

namespace EventSourcing.Core;

public class ProjectionUpdateService : IProjectionUpdateService
{
  private readonly IAggregateService _service;
  private readonly IRecordStore _store;

  public ProjectionUpdateService(IAggregateService service, IRecordStore store)
  {
    _service = service;
    _store = store;
  }

  public async Task UpdateProjectionsAsync(Type aggregateType, Type projectionType, CancellationToken cancellationToken = default)
  {
    var factory = ProjectionCache.FactoryByAggregateAndProjection[(aggregateType, projectionType)];
    var hash = ProjectionCache.Hashes[factory.GetType().Name];

    var items = _store.Projections
      .Where(x =>
        x.AggregateType == aggregateType.Name &&
        x.Type == projectionType.Name &&
        x.Hash != hash)
      .Select(x => new { x.PartitionId, x.AggregateId })
      .AsAsyncEnumerable()
      .WithCancellation(cancellationToken);

    var method = GetRehydrateMethod(aggregateType);
    
    await foreach (var item in items)
    {
      var aggregate = await RehydrateAsync(method, item.PartitionId, item.AggregateId, cancellationToken);
      
      if (aggregate == null) continue;

      await _store.AddProjectionAsync(factory.CreateProjection(aggregate), cancellationToken);
    }
  }
  
  public async Task UpdateProjectionsAsync(Type aggregateType, CancellationToken cancellationToken = default)
  {
    // Get Projection Factories for TAggregate
    var factories = ProjectionCache.FactoriesByAggregate[aggregateType]
      .ToDictionary(x => x.ProjectionType.Name, x => x);
    
    // Het hashes for each of those factories, representing the logic of the aggregate and projection factory
    var hashes = factories.Values.Select(x => ProjectionCache.Hashes[x.GetType().Name]);
    
    var items = _store.Projections

      // Get Outdated Projections, i.e. those whose stored hash differs from the local hash
      // The Contains method assumes there are no hash collisions for the projections of this aggregate type
      .Where(x => x.AggregateType == aggregateType.Name && !hashes.Contains(x.Hash))
      
      // Cosmos Linq does not support the GroupBy method. Hence we're emulating it using OrderBy
      .OrderBy(x => x.AggregateId)
      
      // We need PartitionId and AggregateId for rehydration and Projection Type for the ProjectionFactory
      .Select(x => new { x.PartitionId, x.AggregateId, x.Type })
      
      .AsAsyncEnumerable()
      .WithCancellation(cancellationToken);

    Aggregate? aggregate = null;
    IRecordTransaction? transaction = null;

    var method = GetRehydrateMethod(aggregateType);

    await foreach (var item in items)
    {
      // Prevent Hydrating Aggregates more times than necessary, ties into the OrderBy method of the items query
      if (aggregate == null || aggregate.Id != item.AggregateId)
      {
        // Commit the previous transaction
        if (transaction != null) await transaction.CommitAsync(cancellationToken);

        aggregate = await RehydrateAsync(method, item.PartitionId, item.AggregateId, cancellationToken);
        transaction = _store.CreateTransaction(item.PartitionId);
      }
      
      // If Events are deleted, but Projections remain, it could be that RehydrateAsync returns null
      if (aggregate != null && transaction != null)
        transaction.AddProjection(factories[item.Type].CreateProjection(aggregate));
    }

    // Commit the last transaction
    if (transaction != null) await transaction.CommitAsync(cancellationToken);
  }

  public async Task UpdateProjectionsAsync(CancellationToken cancellationToken = default)
  {
    foreach (var aggregateType in ProjectionCache.FactoriesByAggregate.Keys)
      await UpdateProjectionsAsync(aggregateType, cancellationToken);
  }
  
  private MethodInfo GetRehydrateMethod(Type aggregateType) => _service.GetType()
    .GetMethod(nameof(_service.RehydrateAsync), new[] { typeof(Guid), typeof(Guid), typeof(CancellationToken) })!
    .MakeGenericMethod(aggregateType);
  
  private async Task<Aggregate?> RehydrateAsync(MethodInfo method, Guid partitionId, Guid aggregateId, CancellationToken cancellationToken) =>
    await (dynamic) method.Invoke(_service, new object[] { partitionId, aggregateId, cancellationToken })!;
}