namespace EventSourcing.Core.Records;

public interface IAggregate : IRecord
{
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; }
  
  /// <summary>
  /// The hash representing the logic of this Aggregate.
  /// When this hash is not equal to the hash stored in the Aggregate View, the AggregateView is outdated
  /// </summary>
  public string Hash { get; }
}