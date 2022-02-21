namespace EventSourcing.Core;

public abstract class SnapshotFactory<TAggregate, TSnapshot> : ISnapshotFactory
  where TAggregate : Aggregate where TSnapshot : Snapshot
{
  public Type AggregateType => typeof(TAggregate);
  public Type SnapshotType => typeof(TSnapshot);

  public abstract long SnapshotInterval { get; }
  
  /// <summary>
  /// Calculates if the snapshot interval has been exceeded (and a snapshot thus has to be created)
  /// </summary>
  /// <returns></returns>
  public bool IsSnapshotIntervalExceeded(Aggregate aggregate) =>
    SnapshotInterval != 0 && aggregate.UncommittedEvents.Any() && 
    aggregate.UncommittedEvents.First().Index / SnapshotInterval != 
    (aggregate.UncommittedEvents.Last().Index + 1) / SnapshotInterval;

  protected abstract TSnapshot CreateSnapshot(TAggregate aggregate);

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
}
