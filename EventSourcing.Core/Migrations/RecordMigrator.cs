namespace EventSourcing.Core.Migrations;


public abstract class RecordMigrator<TSource, TTarget> : IRecordMigrator where TSource : Record where TTarget : Record
{
    public Type Source => typeof(TSource);
    public Type Target => typeof(TTarget);
    public Record Convert(Record record) => 
        Convert(record as TSource) with
        {
            Timestamp = record.Timestamp,
            Type = typeof(TTarget).FullName,
            AggregateId = record.AggregateId,
            AggregateType = record.AggregateType,
            AggregateVersion = record.AggregateVersion,
            RecordId = record.RecordId,
            PartitionId = record.PartitionId
        };

    public abstract TTarget Convert(TSource e);
}