using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests.Mocks;

internal record SimpleAggregate : Aggregate
{
  public int Counter { get; private set; }

  protected override void Apply(Event e) => Counter++;
}