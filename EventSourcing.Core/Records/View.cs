using EventSourcing.Core.Services;

namespace EventSourcing.Core.Records;

public abstract record View : IAggregate
{
  public RecordKind Kind { get; init; }
  public string Type { get; init; }
  public Guid PartitionId { get; init; }
  public Guid Id { get; init; }
  
  public long Version { get; init; }
  public string Hash { get; init; }
}

public record View<TAggregate> : View where TAggregate : Aggregate
{
  protected View() => Type = RecordTypeCache.GetAssemblyRecordTypeString(typeof(TAggregate));
}