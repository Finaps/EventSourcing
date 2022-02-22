using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class RecordStoreTests
{
  [Fact]
  public async Task Can_Store_And_View_Aggregate()
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

    await RecordStore.AddViewAsync(new MockAggregateViewFactory().CreateView(aggregate));

    var view = await RecordStore
      .GetViews<MockAggregateView>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .SingleAsync();
    
    Assert.Equal(aggregate.MockBoolean, view.MockBoolean);
    Assert.Equal(aggregate.MockString, view.MockString);
    Assert.Equal(aggregate.MockDecimal, view.MockDecimal);
    Assert.Equal(aggregate.MockDouble, view.MockDouble);
    Assert.Equal(aggregate.MockEnum, view.MockEnum);
    Assert.Equal(aggregate.MockFlagEnum, view.MockFlagEnum);
    Assert.Equal(aggregate.MockNestedRecord, view.MockNestedRecord);
    Assert.Equal(aggregate.MockNestedClassList, view.MockNestedClassList);
    Assert.Equal(aggregate.MockFloatList, view.MockFloatList);
    Assert.Equal(aggregate.MockStringSet, view.MockStringSet);
  }
}