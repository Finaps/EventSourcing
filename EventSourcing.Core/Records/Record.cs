namespace Finaps.EventSourcing.Core;

/// <summary>
/// 
/// </summary>
public enum RecordKind
{
  /// <summary>
  /// Invalid State: <see cref="Record"/> is not a <see cref="EventSourcing.Core.Event"/>, <see cref="EventSourcing.Core.Snapshot"/> or <see cref="EventSourcing.Core.Projection"/>.
  /// </summary>
  None = 0,
  
  /// <summary>
  /// <see cref="Record"/> is a <see cref="EventSourcing.Core.Event"/>
  /// </summary>
  Event = 1,
  
  /// <summary>
  /// <see cref="Record"/> is a <see cref="EventSourcing.Core.Snapshot"/>
  /// </summary>
  Snapshot = 2,
  
  /// <summary>
  /// <see cref="Record"/> is a <see cref="EventSourcing.Core.Projection"/>
  /// </summary>
  Projection = 3
}

/// <summary>
/// Base for <see cref="Event"/>s, <see cref="Snapshot"/>s and <see cref="Projection"/>s
/// </summary>
public abstract record Record
{
  /// <summary>
  /// <see cref="RecordKind"/> of this <see cref="Record"/>.
  /// </summary>
  /// <remarks>
  /// Used to differentiate between <see cref="Record"/> kinds in database queries
  /// </remarks>
  public RecordKind Kind => this switch
  {
    Projection => RecordKind.Projection,
    Snapshot => RecordKind.Snapshot,
    Event => RecordKind.Event,
    _ => RecordKind.None
  };
  
  /// <summary>
  /// String representation of Record Type. Defaults to <c>GetType().Name</c>
  /// </summary>
  /// <remarks>
  /// Can be overridden using <see cref="RecordTypeAttribute"/>
  /// </remarks>
  public string Type { get; init; }
  
  /// <summary>
  /// Aggregate Type string.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Set to <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Type"/> when <see cref="Event"/> is added to an Aggregate.
  /// </para>
  /// </remarks>
  public string? AggregateType { get; init; }
  
  /// <summary>
  /// Unique Partition identifier.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Set to <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.PartitionId"/> when <see cref="Event"/> is added to an Aggregate.
  /// </para>
  /// <para>
  /// <see cref="PartitionId"/> is mapped directly to CosmosDB's <c>PartitionKey</c>.
  /// See https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview for more information.
  /// </para>
  /// <para>
  /// <see cref="IRecordTransaction"/> is scoped to <see cref="PartitionId"/>,
  /// i.e. no transactions involving multiple <see cref="PartitionId"/>'s can be committed.
  /// </para>
  /// </remarks>
  public Guid PartitionId { get; init; }

  /// <summary>
  /// Unique Aggregate identifier.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Set to <see cref="Aggregate{TAggregate}"/>.<see cref="Aggregate{TAggregate}.Id"/> when <see cref="Event"/> is added to an Aggregate.
  /// </para>
  /// </remarks>
  public Guid AggregateId { get; init; }

  /// <summary>
  /// Record creation/update time. Defaults to <see cref="DateTimeOffset"/>.<see cref="DateTimeOffset.Now"/> on creation.
  /// </summary>
  public DateTimeOffset Timestamp { get; init; }
  
  /// <summary>
  /// Unique Database identifier.
  /// </summary>
  public abstract string id { get; }

  /// <summary>
  /// Create new <see cref="Record"/>
  /// </summary>
  protected Record()
  {
    Type = GetType().Name;
    Timestamp = DateTimeOffset.Now;
  }
}