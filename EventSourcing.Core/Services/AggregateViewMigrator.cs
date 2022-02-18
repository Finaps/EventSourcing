using EventSourcing.Core.Records;

namespace EventSourcing.Core.Services;

public class AggregateViewMigrator
{
  private readonly IAggregateService _service;
  private readonly IEventStore _store;

  public AggregateViewMigrator(IAggregateService service, IEventStore store)
  {
    _service = service;
    _store = store;
  }

  public async Task<TAggregate> MigrateViewAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new()
  {
    var aggregate = await _service.RehydrateAsync<TAggregate>(partitionId, aggregateId, cancellationToken);
    
    if (aggregate == null)
      throw new ArgumentException($"Cannot Migrate Aggregate View with PartitionId '{partitionId}' and Id '{aggregateId}': Aggregate does not exist.");

    await _store.AddAsync(aggregate, cancellationToken);

    return aggregate;
  }

  public async Task MigrateViewsAsync<TAggregate>(CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var hash = AggregateHashCache.Get(typeof(TAggregate));
    
    await foreach (var item in _store.Views
                     .Where(x => x.Type == new TAggregate().Type && x.Hash != hash)
                     .Select(x => new { x.PartitionId, x.Id })
                     .AsAsyncEnumerable()
                     .WithCancellation(cancellationToken))
      await MigrateViewAsync<TAggregate>(item.PartitionId, item.Id, cancellationToken);
  }
}