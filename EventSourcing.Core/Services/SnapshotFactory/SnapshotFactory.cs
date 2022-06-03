namespace Finaps.EventSourcing.Core;

/// <summary>
/// Create <typeparamref name="TSnapshot"/> for <typeparamref name="TAggregate"/>
/// </summary>
/// <typeparam name="TAggregate"><see cref="Aggregate{TAggregate}"/> type</typeparam>
/// <typeparam name="TSnapshot"><see cref="Projection"/> type</typeparam>
public abstract class SnapshotFactory<TAggregate, TSnapshot> : ISnapshotFactory
  where TAggregate : Aggregate where TSnapshot : Snapshot
{
  /// <inheritdoc />
  public Type AggregateType => typeof(TAggregate);

  /// <inheritdoc />
  public Type SnapshotType => typeof(TSnapshot);

  /// <inheritdoc />
  public abstract long SnapshotInterval { get; }
  
  /// <inheritdoc />
  public bool IsSnapshotIntervalExceeded(Aggregate aggregate) =>
    SnapshotInterval != 0 && aggregate.UncommittedEvents.Any() && 
    aggregate.UncommittedEvents.First().Index / SnapshotInterval != 
    (aggregate.UncommittedEvents.Last().Index + 1) / SnapshotInterval;

  /// <inheritdoc />
  public Snapshot CreateSnapshot(Aggregate aggregate)
  {
    if (aggregate.Version == 0)
      throw new InvalidOperationException(
        "Error creating snapshot. Snapshots are undefined for aggregates with version 0.");
    
    return CreateSnapshot((TAggregate)aggregate) with
    {
      PartitionId = aggregate.PartitionId,
      AggregateId = aggregate.Id,
      AggregateType = aggregate.Type,
      Index = aggregate.Version - 1
    };
  }

  /// <summary>
  /// Create <typeparamref name="TSnapshot"/> defined for a particular <typeparamref name="TAggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <typeparamref name="TAggregate"/></param>
  /// <returns>Resulting <typeparamref name="TSnapshot"/>s of <typeparamref name="TAggregate"/></returns>
  protected abstract TSnapshot CreateSnapshot(TAggregate aggregate);
}
