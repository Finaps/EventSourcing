namespace Finaps.EventSourcing.Core;

/// <summary>
/// Represents an <see cref="Event"/> that happened to an <see cref="Aggregate{TAggregate}"/>.
/// </summary>
/// <seealso cref="Aggregate{TAggregate}"/>
/// <seealso cref="IRecordStore"/>
public record Event : Record
{
  /// <summary>
  /// The index of this <see cref="Event"/> with respect to <see cref="Record.AggregateId"/>
  /// </summary>
  public long Index { get; init; }

  /// <summary>
  /// Unique Database identifier
  /// </summary>
  public override string id => $"{Kind.ToString()}|{AggregateId}[{Index}]";
}

/// <inheritdoc />
public record Event<TAggregate> : Event where TAggregate : Aggregate, new()
{
  /// <inheritdoc />
  public Event() => AggregateType = typeof(TAggregate).Name;
  
  internal Event<TAggregate>? _previousEvent { get; set; }
}