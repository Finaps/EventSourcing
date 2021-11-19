using System.Collections.Generic;

namespace EventSourcing.Core.Tests.Mocks
{
  public class MockAggregate : Aggregate, IMockAggregate
  {
    public bool MockBoolean { get; set; }
    public string MockString { get; set; }
    public decimal MockDecimal { get; set; }
    public double MockDouble { get; set; }
    
    public MockEnum MockEnum { get; set; }
    public MockFlagEnum MockFlagEnum { get; set; }
    
    public MockNestedClass MockNestedClass { get; set; }
    
    public List<MockNestedClass> MockNestedClassList { get; set; }

    public List<float> MockFloatList { get; set; }
    public HashSet<string> MockStringSet { get; set; }
    
    protected override void Apply<TEvent>(TEvent e)
    {
      switch (e)
      {
        case MockEvent mock:
          MockBoolean = mock.MockBoolean;
          MockString = mock.MockString;
          MockDecimal = mock.MockDecimal;
          MockDouble = mock.MockDouble;
          MockEnum = mock.MockEnum;
          MockFlagEnum = mock.MockFlagEnum;
          MockNestedClass = mock.MockNestedClass;
          MockNestedClassList = mock.MockNestedClassList;
          MockFloatList = mock.MockFloatList;
          MockStringSet = mock.MockStringSet;
          break;
      }
    }
  }
}