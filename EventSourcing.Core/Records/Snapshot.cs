namespace Finaps.EventSourcing.Core;

/// <summary>
/// Represents a <see cref="Snapshot"/> of an <see cref="Aggregate{TAggregate}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots can be used to speed up <see cref="Aggregate{TAggregate}"/> rehydration.
/// </para>
/// <para>
/// To create <see cref="Snapshot"/>s, refer to <see cref="SnapshotFactory{TAggregate,TSnapshot}"/>.
/// Snapshots will be automatically used in rehydration, when available.
/// </para>
/// </remarks>
/// <seealso cref="SnapshotFactory{TAggregate,TSnapshot}"/>
/// <seealso cref="IAggregateService"/>
/// <seealso cref="IRecordStore"/>
public abstract record Snapshot : Record
{
  /// <summary>
  /// The index of this <see cref="Event"/> with respect to <see cref="Record.AggregateId"/>
  /// </summary>
  public long Index { get; init; }
}

/// <inheritdoc />
public record Snapshot<TAggregate> : Snapshot where TAggregate : Aggregate<TAggregate>, new()
{
  /// <inheritdoc />
  public Snapshot() => AggregateType = typeof(TAggregate).Name;
}