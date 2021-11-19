namespace EventSourcing.Core.Tests.Mocks
{
  public class MockAggregateAlternativeView : View<MockAggregate>
  {
    public bool MockBoolean { get; init; }
    public string MockString { get; init; }
    public decimal MockDecimal { get; init; }
    public double MockDouble { get; init; }
  }
}
