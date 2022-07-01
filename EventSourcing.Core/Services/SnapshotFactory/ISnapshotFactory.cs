namespace Finaps.EventSourcing.Core;

public interface ISnapshotFactory
{
  /// <summary>
  /// Source <see cref="Aggregate{TAggregate}"/> type
  /// </summary>
  Type AggregateType { get; }
  
  /// <summary>
  /// Destination <see cref="Snapshot"/> type
  /// </summary>
  Type SnapshotType { get; }
  
  /// <summary>
  /// The interval, in number of <see cref="Event"/>s, for which a <see cref="Snapshot"/> should be created.
  /// </summary>
  public long SnapshotInterval { get; }
}

/// <summary>
/// Create <see cref="Snapshot"/> for <see cref="Aggregate{TAggregate}"/>
/// </summary>
public interface ISnapshotFactory<TAggregate> : ISnapshotFactory
  where TAggregate : Aggregate<TAggregate>, new()
{
  /// <summary>
  /// Calculates if the <see cref="SnapshotInterval"/> has been exceeded (and a <see cref="Snapshot"/> thus has to be created)
  /// </summary>
  /// <param name="aggregate">The <see cref="Aggregate{TAggregate}"/> to check</param>
  /// <returns>True if <see cref="SnapshotInterval"/> has been exceeded</returns>
  bool IsSnapshotIntervalExceeded(TAggregate aggregate);
  
  /// <summary>
  /// Create <see cref="Snapshot"/> defined for a particular <see cref="Aggregate{TAggregate}"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate{TAggregate}"/></param>
  /// <returns>Resulting <see cref="Snapshot"/>s of <see cref="Aggregate{TAggregate}"/></returns>
  Snapshot<TAggregate> CreateSnapshot(Aggregate aggregate);
}