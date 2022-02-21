namespace EventSourcing.Core;

/// <summary>
/// Create <see cref="Snapshot"/> for <see cref="Aggregate"/>
/// </summary>
public interface ISnapshotFactory
{
  Type AggregateType { get; }
  Type SnapshotType { get; }
  
  /// <summary>
  /// The interval, in number of <see cref="Event"/>s, for which a <see cref="Snapshot"/> should be created.
  /// </summary>
  public long SnapshotInterval { get; }
  
  /// <summary>
  /// Calculates if the <see cref="SnapshotInterval"/> has been exceeded (and a <see cref="Snapshot"/> thus has to be created)
  /// </summary>
  /// <param name="aggregate">The <see cref="Aggregate"/> to check</param>
  /// <returns>True if <see cref="SnapshotInterval"/> has been exceeded</returns>
  bool IsSnapshotIntervalExceeded(Aggregate aggregate);
  
  /// <summary>
  /// Create <see cref="Snapshot"/> defined for a particular <see cref="Aggregate"/>
  /// </summary>
  /// <param name="aggregate">Source <see cref="Aggregate"/></param>
  /// <returns>Resulting <see cref="Snapshot"/>s of <see cref="Aggregate"/></returns>
  Snapshot CreateSnapshot(Aggregate aggregate);
}