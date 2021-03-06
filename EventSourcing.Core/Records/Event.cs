namespace Finaps.EventSourcing.Core;

/// <summary>
/// Represents an <see cref="Event"/> that happened to an <see cref="Aggregate{TAggregate}"/>.
/// </summary>
/// <seealso cref="Aggregate{TAggregate}"/>
/// <seealso cref="IRecordStore"/>
public abstract record Event : Record
{
  /// <summary>
  /// The index of this <see cref="Event"/> with respect to <see cref="Record.AggregateId"/>
  /// </summary>
  public long Index { get; init; }
}

/// <inheritdoc />
public record Event<TAggregate> : Event where TAggregate : Aggregate<TAggregate>, new()
{
  /// <inheritdoc />
  public Event() => AggregateType = typeof(TAggregate).Name;
  
  // ReSharper disable once InconsistentNaming
  internal Event<TAggregate>? _previousEvent { get; set; }
}