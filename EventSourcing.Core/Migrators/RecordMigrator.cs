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
    Convert((TSource) record) with {
        Timestamp = record.Timestamp,
        Type = typeof(TTarget).Name,
        AggregateId = record.AggregateId,
        AggregateType = record.AggregateType,
        Index = record.Index,
        RecordId = record.RecordId,
        PartitionId = record.PartitionId
      };

  protected abstract TTarget Convert(TSource e);
}