using System;

namespace EventSourcing.Core
{
  public interface IAggregate : ITyped
  {
    /// <summary>
    /// Unique Aggregate identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// The number of events applied to this aggregate.
    /// </summary>
    public uint Version { get; }

    public string id => Id.ToString();
  }
}