namespace EventSourcing.Core.Migrations;

public abstract class RecordMigrator<TSource, TTarget> : IRecordMigrator
  where TSource : IndexedRecord where TTarget : IndexedRecord
{
  public Type Source => typeof(TSource);
  public Type Target => typeof(TTarget);

  protected RecordMigrator()
  {
    if (Source == Target)
      throw new ArgumentException("Record Migrator Source should not be equal to Target");
  }

  public IndexedRecord Convert(IndexedRecord record) =>
    Convert((TSource)record) with
    {
      Type = RecordTypeCache.GetAssemblyRecordTypeString(Target),
      AggregateType = record.AggregateType,
      
      PartitionId = record.PartitionId,
      AggregateId = record.AggregateId,
      Id = record.Id,
      
      Index = record.Index,
      Timestamp = record.Timestamp
    };

  protected abstract TTarget Convert(TSource e);
}