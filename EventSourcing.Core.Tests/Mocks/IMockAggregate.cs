using System.Collections.Generic;

namespace EventSourcing.Core.Tests.Mocks
{
  public interface IMockAggregate
  {
    public bool MockBoolean { get; }
    public string MockString { get; }
    public decimal MockDecimal { get; }
    public double MockDouble { get; }
    
    public MockEnum MockEnum { get; }
    public MockFlagEnum MockFlagEnum { get; }
    
    public MockNestedClass MockNestedClass { get; }
    
    public List<MockNestedClass> MockNestedClassList { get; }

    public List<float> MockFloatList { get; }
    public HashSet<string> MockStringSet { get; }
  }
}