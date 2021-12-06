using System;

namespace EventSourcing.Core
{
  public abstract class View
  {
    public Guid Id { get; init; }
    public uint Version { get; init; }
    public string Type { get; init; }
    public string Hash { get; init; }
  }

  public class View<TAggregate> : View where TAggregate : Aggregate, new()
  {
    public View()
    {
      Type = new TAggregate().Type;
      Hash = new TAggregate().Hash;
    }
  }
}
