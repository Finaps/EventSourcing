namespace EventSourcing.Core;

public interface ISnapshotFactory
{
  Type AggregateType { get; }
  Type SnapshotType { get; }
  
  bool IsSnapshotIntervalExceeded(Aggregate aggregate);
  
  Snapshot CreateSnapshot(Aggregate aggregate);
}