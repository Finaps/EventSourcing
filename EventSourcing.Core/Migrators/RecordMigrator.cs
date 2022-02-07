namespace EventSourcing.Core.Migrations;

public abstract class RecordMigrator<TSource, TTarget> : IRecordMigrator where TSource : Record where TTarget : Record
{
  public Type Source => typeof(TSource);
  public Type Target => typeof(TTarget);

  protected RecordMigrator()
  {
    if (Source == Target)
      throw new ArgumentException("Record Migrator Source should not be equal to Target");
  }

  public Record Convert(Record record) =>
    Convert(record as TSource) with
    {
      Timestamp = record.Timestamp,
      AggregateId = record.AggregateId,
      AggregateType = record.AggregateType,
      Index = record.Index,
      RecordId = record.RecordId,
      PartitionId = record.PartitionId
    };

  public abstract TTarget Convert(TSource e);
}