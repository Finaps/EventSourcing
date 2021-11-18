namespace EventSourcing.Core.Tests.Mocks
{
  public class EmptyAggregate : Aggregate
  {
    protected override void Apply<TEvent>(TEvent e)
    {
    }
  }
}