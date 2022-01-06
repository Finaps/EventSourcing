namespace EventSourcing.Core.Tests.MockAggregates;

public class EmptyAggregate : Aggregate
{
  protected override void Apply<TEvent>(TEvent e)
  {
  }
}