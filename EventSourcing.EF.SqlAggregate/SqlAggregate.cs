namespace EventSourcing.EF.SqlAggregate;

public abstract record SQLAggregate
{
  public Guid PartitionId { get; init; }
  public Guid AggregateId { get; init; }
  public long Version { get; init; }
}