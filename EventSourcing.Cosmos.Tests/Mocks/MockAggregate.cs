using EventSourcing.Core;

namespace EventSourcing.Cosmos.Tests.Mocks
{
  public class MockAggregate : Aggregate
  {
    protected override void Apply(Event e)
    {
      throw new System.NotImplementedException();
    }
  }
}