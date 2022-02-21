namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="TSnapshot"/> for <see cref="TAggregate"/>
/// </summary>
/// <typeparam name="TAggregate"><see cref="Aggregate"/> type</typeparam>
/// <typeparam name="TSnapshot"><see cref="View"/> type</typeparam>
public abstract class SnapshotFactory<TAggregate, TSnapshot> : ISnapshotFactory
  where TAggregate : Aggregate where TSnapshot : Snapshot
{
  public Type AggregateType => typeof(TAggregate);
  public Type SnapshotType => typeof(TSnapshot);

  public abstract long SnapshotInterval { get; }
  

  public bool IsSnapshotIntervalExceeded(Aggregate aggregate) =>
    SnapshotInterval != 0 && aggregate.UncommittedEvents.Any() && 
    aggregate.UncommittedEvents.First().Index / SnapshotInterval != 
    (aggregate.UncommittedEvents.Last().Index + 1) / SnapshotInterval;
  
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
  /// Create <see cref="TSnapshot"/> defined for a particular <see cref="TAggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="TAggregate"/></param>
  /// <returns>Resulting <see cref="TSnapshot"/>s of <see cref="TAggregate"/></returns>
  protected abstract TSnapshot CreateSnapshot(TAggregate aggregate);
}
