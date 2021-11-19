using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public abstract class ViewStoreTests
  {
    protected abstract IAggregateService GetAggregateService();
    protected abstract IViewStore GetViewStore();

    [Fact]
    public async Task Can_Add_Aggregate()
    {
      var store = GetViewStore();

      var aggregate = new MockAggregate();
      
      aggregate.Add(new MockEvent
      {
        MockBoolean = true,
        MockString = "Hello World",
        MockDecimal = 0.55m,
        MockDouble = 0.6,
        MockEnum = MockEnum.C,
        MockFlagEnum = MockFlagEnum.A | MockFlagEnum.D,
        
        MockNestedClass = new MockNestedClass
        {
          MockBoolean = false,
          MockString = "Bonjour",
          MockDecimal = 0.82m,
          MockDouble = 0.999
        },
        
        MockNestedClassList = new List<MockNestedClass>
        {
          new ()
          {
            MockBoolean = false,
            MockString = "Bye Bye",
            MockDecimal = 99.99m,
            MockDouble = 0.111
          },
        },
        
        MockFloatList = new List<float> { .1f, .5f, .9f },
        MockStringSet = new HashSet<string> { "A", "B", "C", "C" }
      });

      await store.UpdateAsync(aggregate);

      var result = await store.Get<MockAggregate, MockAggregateView>(aggregate.Id);
      
      Assert.Equal(aggregate.MockBoolean, result.MockBoolean);
      Assert.Equal(aggregate.MockString, result.MockString);
      Assert.Equal(aggregate.MockDecimal, result.MockDecimal);
      Assert.Equal(aggregate.MockDouble, result.MockDouble);
      Assert.Equal(aggregate.MockEnum, result.MockEnum);
      Assert.Equal(aggregate.MockFlagEnum, result.MockFlagEnum);
      Assert.Equal(aggregate.MockNestedClass.MockBoolean, result.MockNestedClass.MockBoolean);
      Assert.Equal(aggregate.MockNestedClass.MockString, result.MockNestedClass.MockString);
      Assert.Equal(aggregate.MockNestedClass.MockDecimal, result.MockNestedClass.MockDecimal);
      Assert.Equal(aggregate.MockNestedClass.MockDouble, result.MockNestedClass.MockDouble);
      Assert.Equal(aggregate.MockNestedClassList.Single().MockBoolean, result.MockNestedClassList.Single().MockBoolean);
      Assert.Equal(aggregate.MockNestedClassList.Single().MockString, result.MockNestedClassList.Single().MockString);
      Assert.Equal(aggregate.MockNestedClassList.Single().MockDecimal, result.MockNestedClassList.Single().MockDecimal);
      Assert.Equal(aggregate.MockNestedClassList.Single().MockDouble, result.MockNestedClassList.Single().MockDouble);
      Assert.Equal(aggregate.MockFloatList[0], result.MockFloatList[0]);
      Assert.Equal(aggregate.MockFloatList[1], result.MockFloatList[1]);
      Assert.Equal(aggregate.MockFloatList[2], result.MockFloatList[2]);
      Assert.Contains(result.MockStringSet, x => x == "A");
      Assert.Contains(result.MockStringSet, x => x == "B");
      Assert.Contains(result.MockStringSet, x => x == "C");
    }
    
    [Fact]
    public async Task Can_Get_Alternative_View()
    {
      var store = GetViewStore();

      var aggregate = new MockAggregate();
      
      aggregate.Add(new MockEvent
      {
        MockBoolean = true,
        MockString = "Hello World",
        MockDecimal = 0.55m,
        MockDouble = 0.6,
        MockEnum = MockEnum.C,
        MockFlagEnum = MockFlagEnum.A | MockFlagEnum.D,
        
        MockNestedClass = new MockNestedClass
        {
          MockBoolean = false,
          MockString = "Bonjour",
          MockDecimal = 0.82m,
          MockDouble = 0.999
        },
        
        MockNestedClassList = new List<MockNestedClass>
        {
          new ()
          {
            MockBoolean = false,
            MockString = "Bye Bye",
            MockDecimal = 99.99m,
            MockDouble = 0.111
          },
        },
        
        MockFloatList = new List<float> { .1f, .5f, .9f },
        MockStringSet = new HashSet<string> { "A", "B", "C", "C" }
      });

      await store.UpdateAsync(aggregate);

      var result = await store.Get<MockAggregate, MockAggregateAlternativeView>(aggregate.Id);
      
      Assert.Equal(aggregate.MockBoolean, result.MockBoolean);
      Assert.Equal(aggregate.MockString, result.MockString);
      Assert.Equal(aggregate.MockDecimal, result.MockDecimal);
      Assert.Equal(aggregate.MockDouble, result.MockDouble);
    }

    [Fact]
    public async Task View_Gets_Updated_When_Aggregate_Hash_Changes()
    {
      var store = GetViewStore();
      var service = GetAggregateService();

      var aggregate = new VerboseAggregate { Hash = "DifferentHash" };

      // Add Event and Update View, The View Now Contains One Events
      aggregate.Add(new EmptyEvent());
      await store.UpdateAsync(aggregate);

      // Add another event and only update EventStore
      aggregate.Add(new EmptyEvent());
      await service.PersistAsync(aggregate);

      // Get View, which because the View Hash was 'hacked' at insertion time, will update itself
      var view = await store.Get<VerboseAggregate, VerboseAggregateView>(aggregate.Id);

      Assert.Equal("DifferentHash", aggregate.Hash);
      Assert.Equal("DifferentHash", aggregate.HashDuringApply);

      Assert.NotEqual("DifferentHash", new VerboseAggregate().Hash);
      Assert.Equal(new VerboseAggregate().Hash, view.Hash);
      Assert.Equal(new VerboseAggregate().Hash, view.HashDuringApply);
      Assert.Equal(2, view.NumberOfAppliedEvents);
    }
  }
}