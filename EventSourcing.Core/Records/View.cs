namespace EventSourcing.Core;

public abstract record View : Record
{
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; init; }
}

public record View<TAggregate> : View where TAggregate : Aggregate
{
  public View() => Type = RecordTypeCache.GetAssemblyRecordTypeString(typeof(TAggregate));
}