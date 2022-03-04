namespace EventSourcing.Core;

/// <summary>
/// Represents a <see cref="Snapshot"/> of an <see cref="Aggregate"/>.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots can be used to speed up <see cref="Aggregate"/> rehydration.
/// </para>
/// <para>
/// To create <see cref="Snapshot"/>s, refer to <see cref="SnapshotFactory{TAggregate,TSnapshot}"/>.
/// Snapshots will be automatically used in rehydration, when available.
/// </para>
/// </remarks>
/// <seealso cref="SnapshotFactory{TAggregate,TSnapshot}"/>
/// <seealso cref="IAggregateService"/>
/// <seealso cref="IRecordStore"/>
public record Snapshot : Event;