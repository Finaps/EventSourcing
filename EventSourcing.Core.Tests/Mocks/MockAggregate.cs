using System.Collections.Generic;

namespace EventSourcing.Core.Tests.Mocks
{
  public class MockAggregate : Aggregate, IMockAggregate
  {
    public bool MockBoolean { get; private set; }
    public string MockString { get; private set; }
    public decimal MockDecimal { get; private set; }
    public double MockDouble { get; private set; }
    
    public MockEnum MockEnum { get; private set; }
    public MockFlagEnum MockFlagEnum { get; private set; }
    
    public MockNestedClass MockNestedClass { get; private set; }
    
    public List<MockNestedClass> MockNestedClassList { get; private set; }

    public List<float> MockFloatList { get; private set; }
    public HashSet<string> MockStringSet { get; private set; }
    
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