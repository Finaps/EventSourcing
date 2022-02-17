using EventSourcing.Core.Services;

namespace EventSourcing.Core.Records;

public abstract record View : Record
{
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; init; }

  protected View(string type) => Type = type;
}

public record View<TAggregate> : View where TAggregate : Aggregate
{
  public View() : base(RecordTypeCache.GetAssemblyRecordTypeString(typeof(TAggregate))) { }
}