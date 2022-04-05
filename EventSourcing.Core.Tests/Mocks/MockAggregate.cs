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
  public string? MockNullableString { get; }
  public decimal MockDecimal { get; }
  public double MockDouble { get; }
  public double? MockNullableDouble { get; }
    
  public MockEnum MockEnum { get; }
  public MockFlagEnum MockFlagEnum { get; }
    
  public MockNestedRecord MockNestedRecord { get; }
    
  public List<MockNestedRecordItem> MockNestedRecordList { get; }

  public List<float> MockFloatList { get; }
  public List<string> MockStringSet { get; }

  public static void AssertDefault(IMock mock)
  {
    Assert.Equal(default, mock.MockBoolean);
    Assert.Equal(default, mock.MockString);
    Assert.Equal(default, mock.MockNullableString);
    Assert.Equal(default, mock.MockDecimal);
    Assert.Equal(default, mock.MockDouble);
    Assert.Equal(default, mock.MockNullableDouble);
    Assert.Equal(default, mock.MockEnum);
    Assert.Equal(default, mock.MockFlagEnum);
    Assert.Equal(new MockNestedRecord(), mock.MockNestedRecord);
    Assert.Equal(new List<MockNestedRecordItem>(), mock.MockNestedRecordList);
    Assert.Equal(new List<float>(), mock.MockFloatList);
    Assert.Equal(new List<string>(), mock.MockStringSet);
  }

  public static void AssertEqual(IMock expected, IMock actual)
  {
    Assert.Equal(expected.MockBoolean, actual.MockBoolean);
    Assert.Equal(expected.MockString, actual.MockString);
    Assert.Equal(expected.MockNullableString, actual.MockNullableString);
    Assert.Equal(expected.MockDecimal, actual.MockDecimal);
    Assert.Equal(expected.MockDouble, actual.MockDouble);
    Assert.Equal(expected.MockNullableDouble, actual.MockNullableDouble);
    Assert.Equal(expected.MockEnum, actual.MockEnum);
    Assert.Equal(expected.MockFlagEnum, actual.MockFlagEnum);
    Assert.Equal(expected.MockNestedRecord, actual.MockNestedRecord);
    Assert.Equal(expected.MockNestedRecordList, actual.MockNestedRecordList);
    Assert.Equal(expected.MockFloatList, actual.MockFloatList);
    Assert.Equal(expected.MockStringSet, actual.MockStringSet);
  }
}

public record MockEvent : Event<MockAggregate>, IMock
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public string? MockNullableString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
  public double? MockNullableDouble { get; init; }

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
  public string? MockNullableString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
  public double? MockNullableDouble { get; init; }

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
  public string? MockNullableString { get; private set; }
  public decimal MockDecimal { get; private set; }
  public double MockDouble { get; private set; }
  public double? MockNullableDouble { get; private set; }
  public MockEnum MockEnum { get; private set; }
  public MockFlagEnum MockFlagEnum { get; private set; }
  public MockNestedRecord MockNestedRecord { get; private set; }
  public List<MockNestedRecordItem> MockNestedRecordList { get; private set; }
  public List<float> MockFloatList { get; private set; }
  public List<string> MockStringSet { get; private set; }
  
  public static MockAggregate Create()
  {
    var aggregate = new MockAggregate();
    aggregate.Apply(new MockEvent
    {
      MockBoolean = true,
      MockString = "Hello World",
      MockDecimal = .99m,
      MockDouble = 3.14159265359,
      MockEnum = MockEnum.B,
      MockFlagEnum = MockFlagEnum.C | MockFlagEnum.E,
      MockNestedRecord = new MockNestedRecord
      {
        MockBoolean = false,
        MockString = "Bon Appetit",
        MockDecimal = 9.99m,
        MockDouble = 2.71828
      },
      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new ()
        {
          MockBoolean = true,
          MockString = "Good",
          MockDecimal = 99.99m,
          MockDouble = 1.61803398875
        },
        new ()
        {
          MockBoolean = false,
          MockString = "Bye",
          MockDecimal = 99.99m,
          MockDouble = 1.73205080757
        }
      },
      MockFloatList = new List<float> { .1f, .2f, .3f },
      MockStringSet = new List<string> { "No", "Duplicates", "Duplicates", "Here" }
    });

    return aggregate;
  }
  
  protected override void Apply(Event<MockAggregate> e)
  {
    switch (e)
    {
      case MockEvent m:
        MockBoolean = m.MockBoolean;
        MockString = m.MockString;
        MockNullableString = m.MockNullableString;
        MockDecimal = m.MockDecimal;
        MockDouble = m.MockDouble;
        MockNullableDouble = m.MockNullableDouble;
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
  public string? MockNullableString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
  public double? MockNullableDouble { get; init; }
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
    MockNullableString = aggregate.MockNullableString,
    MockDecimal = aggregate.MockDecimal,
    MockDouble = aggregate.MockDouble,
    MockNullableDouble = aggregate.MockNullableDouble,
    MockEnum = aggregate.MockEnum,
    MockFlagEnum = aggregate.MockFlagEnum,
    MockNestedRecord = aggregate.MockNestedRecord,
    MockNestedRecordList = aggregate.MockNestedRecordList,
    MockFloatList = aggregate.MockFloatList,
    MockStringSet = aggregate.MockStringSet
  };
}
