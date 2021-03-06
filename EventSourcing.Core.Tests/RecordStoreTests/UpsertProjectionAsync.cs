namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_UpsertProjectionAsync_Can_Store_And_Project_Aggregate()
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
      MockNestedRecord = new MockNestedRecord(false, "Bon Appetit", 9.99m, 2.71828),
      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new (true, "Good", 99.99m, 1.61803398875),
        new (false, "Bye", 99.99m, 1.73205080757)
      },
      MockFloatList = new List<float> { .1f, .2f, .3f },
      MockStringSet = new List<string> { "No", "Duplicates", "Duplicates", "Here" }
    });

    // Create Projection
    await GetRecordStore().UpsertProjectionAsync(new MockAggregateProjectionFactory().CreateProjection(aggregate)!);
    
    var projection = await GetRecordStore()
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
    Assert.Equal(aggregate.MockNestedRecordList, projection.MockNestedRecordList);
    Assert.Equal(aggregate.MockFloatList, projection.MockFloatList);
    Assert.Equal(aggregate.MockStringSet, projection.MockStringSet);
    
    aggregate.Apply(new MockEvent
    {
      MockBoolean = true,
      MockString = "Hello World",
      MockDecimal = .99m,
      MockDouble = 3.14159265359,
      MockEnum = MockEnum.B,
      MockFlagEnum = MockFlagEnum.C | MockFlagEnum.E,
      MockNestedRecord = new MockNestedRecord(false, "Bon Appetit", 9.99m, 2.71828),
      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new (true, "Good", 99.99m, 1.61803398875),
        new (false, "Bye", 99.99m, 1.73205080757)
      },
      MockFloatList = new List<float> { .1f, .2f, .3f },
      MockStringSet = new List<string> { "No", "Duplicates", "Duplicates", "Here" }
    });

    // Update Projection
    await GetRecordStore().UpsertProjectionAsync(new MockAggregateProjectionFactory().CreateProjection(aggregate)!);
    
    var updatedProjection = await GetRecordStore()
      .GetProjections<MockAggregateProjection>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    Assert.True(updatedProjection.IsUpToDate);
    Assert.Equal(aggregate.MockBoolean, updatedProjection.MockBoolean);
    Assert.Equal(aggregate.MockString, updatedProjection.MockString);
    Assert.Equal(aggregate.MockDecimal, updatedProjection.MockDecimal);
    Assert.Equal(aggregate.MockDouble, updatedProjection.MockDouble);
    Assert.Equal(aggregate.MockEnum, updatedProjection.MockEnum);
    Assert.Equal(aggregate.MockFlagEnum, updatedProjection.MockFlagEnum);
    Assert.Equal(aggregate.MockNestedRecord, updatedProjection.MockNestedRecord);
    Assert.Equal(aggregate.MockNestedRecordList, updatedProjection.MockNestedRecordList);
    Assert.Equal(aggregate.MockFloatList, updatedProjection.MockFloatList);
    Assert.Equal(aggregate.MockStringSet, updatedProjection.MockStringSet);
  }
}