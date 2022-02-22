namespace EventSourcing.Core;

public abstract class EventMigrator<TSource, TTarget> : IEventMigrator
  where TSource : Event where TTarget : Event
{
  public Type Source => typeof(TSource);
  public Type Target => typeof(TTarget);

  protected EventMigrator()
  {
    if (Source == Target)
      throw new ArgumentException("Record Migrator Source should not be equal to Target");
  }

  public Event Convert(Event record) =>
    Convert((TSource) record) with
    {
      Type = RecordTypeCache.GetAssemblyRecordTypeString(Target),
      AggregateType = record.AggregateType,
      
      PartitionId = record.PartitionId,
      AggregateId = record.AggregateId,
      RecordId = record.RecordId,
      
      Index = record.Index,
      Timestamp = record.Timestamp
    };

  protected abstract TTarget Convert(TSource e);
}