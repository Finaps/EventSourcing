namespace EventSourcing.Core.Records;

/// <summary>
/// Base Indexed Record, representing the common properties between <see cref="Event"/>s and <see cref="Snapshot"/>s.
/// </summary>
public record IndexedRecord : Record, IIndexedRecord
{
  public string? AggregateType { get; init; }
  public Guid AggregateId { get; init; }
  public long Index { get; init; }
  public DateTimeOffset Timestamp { get; init; }
  public override string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";
  protected IndexedRecord() => Timestamp = DateTimeOffset.Now;
}