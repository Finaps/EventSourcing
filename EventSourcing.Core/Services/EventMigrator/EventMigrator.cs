namespace EventSourcing.Core;

/// <inheritdoc />
public abstract class EventMigrator<TSource, TTarget> : IEventMigrator
  where TSource : Event where TTarget : Event
{
  /// <inheritdoc />
  public Type Source => typeof(TSource);

  /// <inheritdoc />
  public Type Target => typeof(TTarget);

  protected EventMigrator()
  {
    if (Source == Target)
      throw new ArgumentException("Record Migrator Source should not be equal to Target");
  }

  /// <inheritdoc />
  public Event Migrate(Event e) =>
    Migrate((TSource) e) with
    {
      Type = RecordTypeCache.GetAssemblyRecordTypeString(Target),
      AggregateType = e.AggregateType,
      
      PartitionId = e.PartitionId,
      AggregateId = e.AggregateId,

      Index = e.Index,
      Timestamp = e.Timestamp
    };

  /// <summary>
  /// Migrate an <see cref="Event"/> to a newer schema version
  /// </summary>
  /// <param name="e"><see cref="Event"/> to migrate</param>
  /// <returns>Migrated <see cref="Event"/></returns>
  protected abstract TTarget Migrate(TSource e);
}