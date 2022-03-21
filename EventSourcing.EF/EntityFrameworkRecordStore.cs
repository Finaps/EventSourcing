using EventSourcing.Core;

namespace EventSourcing.EF;

public class EntityFrameworkRecordStore : IRecordStore
{
  internal static List<Type> ProjectionTypes { get; } = new();
  internal RecordContext Context { get; set; }

  public EntityFrameworkRecordStore(RecordContext context) => Context = context;

  public IQueryable<Event> GetEvents<TAggregate>() where TAggregate : Aggregate, new() => Context.Set<Event<TAggregate>>();
  public IQueryable<Snapshot> GetSnapshots<TAggregate>() where TAggregate : Aggregate, new() => Context.Set<Snapshot<TAggregate>>();
  public IQueryable<TProjection> GetProjections<TProjection>() where TProjection : Projection, new() => Context.Set<TProjection>();

  public async Task<TProjection?> GetProjectionByIdAsync<TProjection>(Guid partitionId, Guid aggregateId,
    CancellationToken cancellationToken = default) where TProjection : Projection, new() =>
    await Context.Set<TProjection>().FindAsync(new object?[] { partitionId, aggregateId }, cancellationToken);

  public async Task AddEventsAsync(IList<Event> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return;
    
    await CreateTransaction(events.First().PartitionId)
      .AddEvents(events)
      .CommitAsync(cancellationToken);
  }
  
  public async Task AddSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default) =>
    await CreateTransaction(snapshot.PartitionId)
      .AddSnapshot(snapshot)
      .CommitAsync(cancellationToken);

  public async Task UpsertProjectionAsync(Projection projection, CancellationToken cancellationToken = default) =>
    await CreateTransaction(projection.PartitionId)
      .UpsertProjection(projection)
      .CommitAsync(cancellationToken);

  public async Task DeleteAllEventsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await Context.DeleteWhereAsync(typeof(TAggregate).EventTable(), partitionId, aggregateId, cancellationToken);

  public async Task DeleteAllSnapshotsAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await Context.DeleteWhereAsync(typeof(TAggregate).SnapshotTable(), partitionId, aggregateId, cancellationToken);

  public async Task DeleteSnapshotAsync<TAggregate>(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new() =>
    await Context.DeleteWhereAsync(typeof(TAggregate).SnapshotTable(), partitionId, aggregateId, index, cancellationToken);

  public async Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new()
  {
    var projection = new TProjection { PartitionId = partitionId, AggregateId = aggregateId };
    Context.Set<TProjection>().Attach(projection);
    Context.Set<TProjection>().Remove(projection);
    await Context.SaveChangesAsync(cancellationToken);
  }

  public async Task<int> DeleteAggregateAsync<TAggregate>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
  {
    var count = 0;
    
    count += await Context.DeleteWhereAsync(typeof(TAggregate).EventTable(), partitionId, aggregateId, cancellationToken);
    count += await Context.DeleteWhereAsync(typeof(TAggregate).SnapshotTable(), partitionId, aggregateId, cancellationToken);
    
    foreach (var type in ProjectionTypes)
      count += await Context.DeleteWhereAsync(type.Name, partitionId, aggregateId, cancellationToken);

    return count;
  }

  public IRecordTransaction CreateTransaction(Guid partitionId) =>
    new EntityFrameworkRecordTransaction(this, partitionId);
}