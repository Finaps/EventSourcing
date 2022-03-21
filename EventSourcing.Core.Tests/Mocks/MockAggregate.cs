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
  public string? MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
}

public record MockNestedRecordItem
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
    
  public List<MockNestedRecordItem> MockNestedRecordList { get; }

  public List<float> MockFloatList { get; }
  public List<string> MockStringSet { get; }
}

public record MockEvent : Event<MockAggregate>, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
    
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
    
  public MockNestedRecord MockNestedRecord { get; init; }
    
  public List<MockNestedRecordItem> MockNestedRecordList { get; init; }

  public List<float> MockFloatList { get; init; }
  public List<string> MockStringSet { get; init; }
}

public record MockSnapshot : Snapshot<MockAggregate>, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
    
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
    
  public MockNestedRecord MockNestedRecord { get; init; }
    
  public List<MockNestedRecordItem> MockNestedRecordList { get; init; }

  public List<float> MockFloatList { get; init; }
  public List<string> MockStringSet { get; init; }
}

public class MockAggregate : Aggregate<MockAggregate>, IMock
{
  public Guid NiceRelation { get; private set; }
  public bool MockBoolean { get; private set; }
  public string MockString { get; private set; }
  public decimal MockDecimal { get; private set; }
  public double MockDouble { get; private set; }
  public MockEnum MockEnum { get; private set; }
  public MockFlagEnum MockFlagEnum { get; private set; }
  public MockNestedRecord MockNestedRecord { get; private set; }
  public List<MockNestedRecordItem> MockNestedRecordList { get; private set; }
  public List<float> MockFloatList { get; private set; }
  public List<string> MockStringSet { get; private set; }
  
  protected override void Apply(Event<MockAggregate> e)
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
        MockNestedRecordList = m.MockNestedRecordList;
        MockFloatList = m.MockFloatList;
        MockStringSet = m.MockStringSet;
        break;
    }
  }
}

public record MockAggregateProjection : Projection, IMock
{
  public bool MockBoolean { get; init; }
  public string? MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
  public MockNestedRecord MockNestedRecord { get; init; }
  public List<MockNestedRecordItem> MockNestedRecordList { get; init; }
  public List<float> MockFloatList { get; init; }
  public List<string> MockStringSet { get; init; }
}

public class MockAggregateProjectionFactory : ProjectionFactory<MockAggregate, MockAggregateProjection>
{
  protected override MockAggregateProjection CreateProjection(MockAggregate aggregate) => new()
  {
    MockBoolean = aggregate.MockBoolean,
    MockString = aggregate.MockString,
    MockDecimal = aggregate.MockDecimal,
    MockDouble = aggregate.MockDouble,
    MockEnum = aggregate.MockEnum,
    MockFlagEnum = aggregate.MockFlagEnum,
    MockNestedRecord = aggregate.MockNestedRecord,
    MockNestedRecordList = aggregate.MockNestedRecordList,
    MockFloatList = aggregate.MockFloatList,
    MockStringSet = aggregate.MockStringSet
  };
}
