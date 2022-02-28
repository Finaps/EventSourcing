using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class AggregateServiceTests
{
  private static MockAggregate CreateMockAggregate()
  {
    var aggregate = new MockAggregate();
    aggregate.Add(new MockEvent
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
      MockNestedClassList = new List<MockNestedRecord>
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
      MockStringSet = new HashSet<string> { "No", "Duplicates", "Duplicates", "Here" }
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
    Assert.Equal(default, projection.MockNestedRecord);
    Assert.Equal(default, projection.MockNestedClassList);
    Assert.Equal(default, projection.MockFloatList);
    Assert.Equal(default, projection.MockStringSet);
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
    Assert.Equal(expected.MockNestedClassList, actual.MockNestedClassList);
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

    await RecordStore.AddProjectionAsync(new MockAggregateProjection
    {
      AggregateType = aggregate.Type,
      AggregateId = aggregate.Id,
      FactoryType = nameof(MockAggregateProjectionFactory),
      Hash = "OUTDATED"
    });
    
    var before = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    AssertDefaultMock(before);

    await ProjectionUpdateService.UpdateProjectionsAsync<MockAggregate, MockAggregateProjection>();

    var after = await RecordStore
    .GetProjections<MockAggregateProjection>()
    .Where(x => x.AggregateId == aggregate.Id)
    .AsAsyncEnumerable()
    .SingleAsync();
  
    AssertEqualMock(aggregate, after);
  }
  
  [Fact]
  public async Task Can_Update_Projection_By_Aggregate()
  {
    var aggregate = CreateMockAggregate();

    await AggregateService.PersistAsync(aggregate);

    await RecordStore.AddProjectionAsync(new MockAggregateProjection
    {
      AggregateType = aggregate.Type,
      AggregateId = aggregate.Id,
      FactoryType = nameof(MockAggregateProjectionFactory),
      Hash = "OUTDATED"
    });
    
    var before = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    AssertDefaultMock(before);

    await ProjectionUpdateService.UpdateProjectionsAsync<MockAggregate>();

    var after = await RecordStore
    .GetProjections<MockAggregateProjection>()
    .Where(x => x.AggregateId == aggregate.Id)
    .AsAsyncEnumerable()
    .SingleAsync();
  
    AssertEqualMock(aggregate, after);
  }
  
  [Fact]
  public async Task Can_Update_Projection_For_All_Aggregates()
  {
    var aggregate = CreateMockAggregate();

    await AggregateService.PersistAsync(aggregate);

    await RecordStore.AddProjectionAsync(new MockAggregateProjection
    {
      AggregateType = aggregate.Type,
      AggregateId = aggregate.Id,
      FactoryType = nameof(MockAggregateProjectionFactory),
      Hash = "OUTDATED"
    });
    
    var before = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    AssertDefaultMock(before);

    await ProjectionUpdateService.UpdateProjectionsAsync();

    var after = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
  
    AssertEqualMock(aggregate, after);
  }
}