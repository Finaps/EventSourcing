namespace EventSourcing.Core;

public class AggregateService : IAggregateService
{
  public static Dictionary<Type, string> AggregateHashCache = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(Aggregate).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && type.IsPublic)
    .ToDictionary(type => type, type => ((Aggregate)Activator.CreateInstance(type)!).ComputeHash());

  private readonly IRecordStore _store;
  public AggregateService(IRecordStore store) => _store = store;

  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await RehydrateAsync<TAggregate>(partitionId, aggregateId, DateTimeOffset.MaxValue, cancellationToken);

  public async Task<TAggregate?> RehydrateAsync<TAggregate>(Guid partitionId, Guid aggregateId, DateTimeOffset date,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  { 
    var snapshot = await _store.Snapshots
      .Where(x => 
        x.PartitionId == partitionId &&
        x.AggregateId == aggregateId &&
        x.Timestamp <= date)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable()
      .LastOrDefaultAsync(cancellationToken);

    var index = snapshot?.Index ?? -1;

    var events = _store.Events
      .Where(x =>
        x.PartitionId == partitionId &&
        x.AggregateId == aggregateId &&
        x.Timestamp <= date &&
        x.Index > index)
      .OrderBy(x => x.Index)
      .AsAsyncEnumerable();

    return await Aggregate.RehydrateAsync<TAggregate>(partitionId, aggregateId, snapshot, events, cancellationToken);
  }

  public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    await CreateTransaction(aggregate.PartitionId).Add(aggregate).CommitAsync(cancellationToken);
    
    return aggregate;
  }

  public async Task PersistAsync(IEnumerable<Aggregate> aggregates, CancellationToken cancellationToken = default)
  {
    IAggregateTransaction? transaction = null;
    
    foreach (var aggregate in aggregates)
    {
      transaction ??= CreateTransaction(aggregate.PartitionId);
      transaction.Add(aggregate);
    }

    if (transaction != null)
      await transaction.CommitAsync(cancellationToken);
  }

  public IAggregateTransaction CreateTransaction(Guid partitionId) => new AggregateTransaction(_store.CreateTransaction(partitionId));
  public IAggregateTransaction CreateTransaction() => CreateTransaction(Guid.Empty);
}