using System;

namespace EventSourcing.Core
{
  public interface IEvent : ITyped
  {
    /// <summary>
    /// Unique Event identifier
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Event creation time
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Unique Aggregate identifier
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Aggregate type
    /// </summary>
    public string AggregateType { get; init; }
    
    /// <summary>
    /// Index of this Event in the Aggregate Event Stream
    /// </summary>
    public uint AggregateVersion { get; init; }
    
    public string id => AggregateVersion.ToString();
  }
}