using EventSourcing.Core.Services;

namespace EventSourcing.Core.Records;

public interface IRecord
{
  /// <summary>
  /// <see cref="RecordKind"/> of this Record. used to discern between Record kinds in database queries
  /// </summary>
  public RecordKind Kind { get; }
  
  /// <summary>
  /// String representation of Record Type
  /// </summary>
  /// <remarks>
  /// Can be overridden using <see cref="RecordTypeAttribute"/>
  /// </remarks>
  public string Type { get; }
  
  /// <summary>
  /// Unique Partition identifier.
  /// </summary>
  /// <remarks>
  /// <see cref="ITransaction"/> and <see cref="IAggregateTransaction"/> are scoped to <see cref="PartitionId"/>
  /// </remarks>
  public Guid PartitionId { get; }
  
  /// <summary>
  /// Unique Record identifier.
  /// </summary>
  public Guid Id { get; }
}