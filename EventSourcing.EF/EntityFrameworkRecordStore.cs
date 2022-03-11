using System.Text.Json;
using EventSourcing.Core;
using EventSourcing.EntityFramework;

namespace EventSourcing.EF;

public class EntityFrameworkRecordStore : IRecordStore
{
  internal static List<Type> ProjectionTypes { get; } = new();
  internal RecordContext Context { get; set; }
  internal RecordSerializer Serializer { get; set; }

  public EntityFrameworkRecordStore(RecordContext context, RecordConverterOptions? options = null)
  {
    Context = context;
    Serializer = new RecordSerializer(new JsonSerializerOptions
    {
      Converters = { new RecordConverter<Event>(options), new RecordConverter<Snapshot>(options) }
    });
  }

  public IQueryable<Event> Events => new EntityFrameworkAsyncQueryable<Event>(Context.Set<EventEntity>());
  public IQueryable<Snapshot> Snapshots => new EntityFrameworkAsyncQueryable<Snapshot>(Context.Set<SnapshotEntity>());
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

  public async Task DeleteAllEventsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) =>
    await Context.DeleteWhereAsync(nameof(EventEntity), partitionId, aggregateId, cancellationToken);

  public async Task DeleteAllSnapshotsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) =>
    await Context.DeleteWhereAsync(nameof(SnapshotEntity), partitionId, aggregateId, cancellationToken);

  public async Task DeleteSnapshotAsync(Guid partitionId, Guid aggregateId, long index, CancellationToken cancellationToken = default) =>
    await Context.DeleteWhereAsync(nameof(SnapshotEntity), partitionId, aggregateId, index, cancellationToken);

  public async Task DeleteAllProjectionsAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    foreach (var type in ProjectionTypes)
      await Context.DeleteWhereAsync(type.Name, partitionId, aggregateId, cancellationToken);
  }

  public async Task DeleteProjectionAsync<TProjection>(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default) where TProjection : Projection, new()
  {
    var projection = new TProjection { PartitionId = partitionId, AggregateId = aggregateId };
    Context.Set<TProjection>().Attach(projection);
    Context.Set<TProjection>().Remove(projection);
    await Context.SaveChangesAsync(cancellationToken);
  }

  public async Task<int> DeleteAggregateAsync(Guid partitionId, Guid aggregateId, CancellationToken cancellationToken = default)
  {
    var count = 0;
    
    count += await Context.DeleteWhereAsync(nameof(EventEntity), partitionId, aggregateId, cancellationToken);
    count += await Context.DeleteWhereAsync(nameof(SnapshotEntity), partitionId, aggregateId, cancellationToken);
    
    foreach (var type in ProjectionTypes)
      count += await Context.DeleteWhereAsync(type.Name, partitionId, aggregateId, cancellationToken);

    return count;
  }

  public IRecordTransaction CreateTransaction(Guid partitionId) =>
    new EntityFrameworkRecordTransaction(this, partitionId);
}