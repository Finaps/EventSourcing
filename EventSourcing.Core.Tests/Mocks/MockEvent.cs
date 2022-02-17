using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests.Mocks;

public enum MockEnum
{
  A = 0,
  B = 1,
  C = 2
}

[Flags]
public enum MockFlagEnum : byte
{
  A = 1 << 0,
  B = 1 << 1,
  C = 1 << 2,
  D = 1 << 3,
  E = 1 << 4
}

public record MockNestedRecord
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
}

public interface IMock
{
  public bool MockBoolean { get; }
  public string MockString { get; }
  public decimal MockDecimal { get; }
  public double MockDouble { get; }
    
  public MockEnum MockEnum { get; }
  public MockFlagEnum MockFlagEnum { get; }
    
  public MockNestedRecord MockNestedRecord { get; }
    
  public List<MockNestedRecord> MockNestedClassList { get; }

  public List<float> MockFloatList { get; }
  public HashSet<string> MockStringSet { get; }
}

public record MockEvent : Event, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
    
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
    
  public MockNestedRecord MockNestedRecord { get; init; }
    
  public List<MockNestedRecord> MockNestedClassList { get; init; }

  public List<float> MockFloatList { get; init; }
  public HashSet<string> MockStringSet { get; init; }
}

public record MockSnapshot : Snapshot, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
    
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
    
  public MockNestedRecord MockNestedRecord { get; init; }
    
  public List<MockNestedRecord> MockNestedClassList { get; init; }

  public List<float> MockFloatList { get; init; }
  public HashSet<string> MockStringSet { get; init; }
}

public record MockAggregate : Aggregate, IMock
{
  public bool MockBoolean { get; private set; }
  public string MockString { get; private set; }
  public decimal MockDecimal { get; private set; }
  public double MockDouble { get; private set; }
  public MockEnum MockEnum { get; private set; }
  public MockFlagEnum MockFlagEnum { get; private set; }
  public MockNestedRecord MockNestedRecord { get; private set; }
  public List<MockNestedRecord> MockNestedClassList { get; private set; }
  public List<float> MockFloatList { get; private set; }
  public HashSet<string> MockStringSet { get; private set; }
  
  protected override void Apply(Event e)
  {
    switch (e)
    {
      case MockEvent m:
        MockBoolean = m.MockBoolean;
        MockString = m.MockString;
        MockDecimal = m.MockDecimal;
        MockDouble = m.MockDouble;
        MockEnum = m.MockEnum;
        MockFlagEnum = m.MockFlagEnum;
        MockNestedRecord = m.MockNestedRecord;
        MockNestedClassList = m.MockNestedClassList;
        MockFloatList = m.MockFloatList;
        MockStringSet = m.MockStringSet;
        break;
    }
  }
}

public record MockAggregateView : View<MockAggregate>, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
  public MockNestedRecord MockNestedRecord { get; init; }
  public List<MockNestedRecord> MockNestedClassList { get; init; }
  public List<float> MockFloatList { get; init; }
  public HashSet<string> MockStringSet { get; init; }
}
