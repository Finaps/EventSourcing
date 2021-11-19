using System;

namespace EventSourcing.Core
{
  public class View<TAggregate> where TAggregate : Aggregate, new()
  {
    public Guid Id { get; init; }
    public uint Version { get; init; }
    public string Type { get; init; }
    public string Hash { get; init; }
  }
}
