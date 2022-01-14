namespace EventSourcing.Core.Tests.Mocks;

internal class SimpleAggregate : Aggregate
{
  public int Counter { get; private set; }

  protected override void Apply(Event e) => Counter++;
}