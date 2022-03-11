using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  private static MockAggregate CreateMockAggregate()
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

  private static void AssertDefaultMock(IMock projection)
  {
    Assert.Equal(default, projection.MockBoolean);
    Assert.Equal(default, projection.MockString);
    Assert.Equal(default, projection.MockDecimal);
    Assert.Equal(default, projection.MockDouble);
    Assert.Equal(default, projection.MockEnum);
    Assert.Equal(default, projection.MockFlagEnum);
    Assert.Equal(new MockNestedRecord(), projection.MockNestedRecord);
    Assert.Equal(new List<MockNestedRecordItem>(), projection.MockNestedRecordList);
    Assert.Equal(new List<float>(), projection.MockFloatList);
    Assert.Equal(new List<string>(), projection.MockStringSet);
  }

  private static void AssertEqualMock(IMock expected, IMock actual)
  {
    Assert.Equal(expected.MockBoolean, actual.MockBoolean);
    Assert.Equal(expected.MockString, actual.MockString);
    Assert.Equal(expected.MockDecimal, actual.MockDecimal);
    Assert.Equal(expected.MockDouble, actual.MockDouble);
    Assert.Equal(expected.MockEnum, actual.MockEnum);
    Assert.Equal(expected.MockFlagEnum, actual.MockFlagEnum);
    Assert.Equal(expected.MockNestedRecord, actual.MockNestedRecord);
    Assert.Equal(expected.MockNestedRecordList, actual.MockNestedRecordList);
    Assert.Equal(expected.MockFloatList, actual.MockFloatList);
    Assert.Equal(expected.MockStringSet, actual.MockStringSet);
  }
  
  [Fact]
  public async Task Can_Query_Aggregate_Projection_After_Persisting()
  {
    var aggregate = CreateMockAggregate();

    await AggregateService.PersistAsync(aggregate);

    var projection = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();

    AssertEqualMock(aggregate, projection);
  }
  
  [Fact]
  public async Task Can_Update_Projection_By_Aggregate_And_Projection()
  {
    var aggregate = CreateMockAggregate();

    await AggregateService.PersistAsync(aggregate);

    await RecordStore.UpsertProjectionAsync(new MockAggregateProjection
    {
      AggregateType = aggregate.Type,
      AggregateId = aggregate.Id,
      FactoryType = nameof(MockAggregateProjectionFactory),
      Hash = "OUTDATED",
      
      MockNestedRecord = new MockNestedRecord(),
      MockNestedRecordList = new List<MockNestedRecordItem>(),
      MockFloatList = new List<float>(),
      MockStringSet = new List<string>()
    });
    
    var before = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    AssertDefaultMock(before);

    await ProjectionUpdateService.UpdateAllProjectionsAsync<MockAggregate, MockAggregateProjection>();

    var after = await RecordStore
    .GetProjections<MockAggregateProjection>()
    .Where(x => x.AggregateId == aggregate.Id)
    .AsAsyncEnumerable()
    .SingleAsync();
  
    AssertEqualMock(aggregate, after);
  }
}