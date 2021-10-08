using EventSourcing.Core;

namespace EventSourcing.Cosmos.Tests.Mocks
{
  public class EmptyAggregate : Aggregate
  {
    protected override void Apply<TEvent>(TEvent e) { }
  }
}