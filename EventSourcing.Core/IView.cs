using System;

namespace EventSourcing.Core
{
  public interface IView
  {
    /// <summary>
    /// Unique Aggregate identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// The number of events applied to this aggregate.
    /// </summary>
    public uint Version { get; init; }
    
    /// <summary>
    /// Aggregate type
    /// </summary>
    public string Type { get; init; }
  }
  public interface IView<TAggregate> : IView where TAggregate : Aggregate { }
}