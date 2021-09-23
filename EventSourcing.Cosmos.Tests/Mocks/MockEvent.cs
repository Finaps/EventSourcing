using EventSourcing.Core;

namespace EventSourcing.Cosmos.Tests.Mocks
{
  public class MockEvent : Event
  {
    public bool MockBoolean { get; set; }
    public int MockInteger { get; set; }
    public double MockDouble { get; set; }
    public string MockString { get; set; }
    
    public MockEvent() : base() { }
    public MockEvent(MockAggregate aggregate) : base(aggregate) { }
  }
}