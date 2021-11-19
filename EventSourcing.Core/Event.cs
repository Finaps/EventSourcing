using System;

namespace EventSourcing.Core
{
  /// <summary>
  /// Base Event
  /// </summary>
  public record Event : ITyped
  {
    /// <summary>
    /// Unique Event identifier
    /// </summary>
    public Guid EventId { get; init; }
    
    /// <summary>
    /// Event type
    /// </summary>
    public string Type { get; init; }
    
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

    public Event()
    {
      EventId = Guid.NewGuid();
      Type = GetType().FullName;
      Timestamp = DateTimeOffset.Now;
    }
  }
}