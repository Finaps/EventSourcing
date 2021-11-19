namespace EventSourcing.Core.Tests.Mocks
{
  public class SimpleAggregateView : View<SimpleAggregate>
  {
    public int Counter { get;init; }
  }
}