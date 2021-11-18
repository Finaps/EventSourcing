using System;

namespace EventSourcing.Core
{
  public class View<TAggregate> : IView<TAggregate> where TAggregate : Aggregate
  {
    public Guid Id { get; init; }
    public uint Version { get; init; }
    public string Type { get; init; }

    public View()
    {
      Type = typeof(TAggregate).Name;
    }
  }
}
