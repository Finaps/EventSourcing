using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  [Fact]
  public async Task Can_Store_And_Project_Aggregate()
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

    await RecordStore.UpsertProjectionAsync(new MockAggregateProjectionFactory().CreateProjection(aggregate));

    var projection = await RecordStore
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    Assert.True(projection.IsUpToDate);
    Assert.Equal(aggregate.MockBoolean, projection.MockBoolean);
    Assert.Equal(aggregate.MockString, projection.MockString);
    Assert.Equal(aggregate.MockDecimal, projection.MockDecimal);
    Assert.Equal(aggregate.MockDouble, projection.MockDouble);
    Assert.Equal(aggregate.MockEnum, projection.MockEnum);
    Assert.Equal(aggregate.MockFlagEnum, projection.MockFlagEnum);
    Assert.Equal(aggregate.MockNestedRecord, projection.MockNestedRecord);
    Assert.Equal(aggregate.MockNestedClassList, projection.MockNestedClassList);
    Assert.Equal(aggregate.MockFloatList, projection.MockFloatList);
    Assert.Equal(aggregate.MockStringSet, projection.MockStringSet);
  }
}