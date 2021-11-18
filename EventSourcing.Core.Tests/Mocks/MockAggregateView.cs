using System.Collections.Generic;

namespace EventSourcing.Core.Tests.Mocks
{
  public class MockAggregateView : View<MockAggregate>, IMockAggregate
  {
    public bool MockBoolean { get; init; }
    public string MockString { get; init; }
    public decimal MockDecimal { get; init; }
    public double MockDouble { get; init; }
    public MockEnum MockEnum { get; init; }
    public MockFlagEnum MockFlagEnum { get; init; }
    public MockNestedClass MockNestedClass { get; init; }
    public List<MockNestedClass> MockNestedClassList { get; init; }
    public List<float> MockFloatList { get; init; }
    public HashSet<string> MockStringSet { get; init; }
  }
}
