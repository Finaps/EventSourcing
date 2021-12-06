using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public abstract class ViewStoreTests
  {
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

      await store.UpsertAsync(aggregate);

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

      await store.UpsertAsync(aggregate);

      var result = await store.Get<MockAggregate, MockAggregateAlternativeView>(aggregate.Id);
      
      Assert.Equal(aggregate.MockBoolean, result.MockBoolean);
      Assert.Equal(aggregate.MockString, result.MockString);
      Assert.Equal(aggregate.MockDecimal, result.MockDecimal);
      Assert.Equal(aggregate.MockDouble, result.MockDouble);
    }

    [Fact]
    public async Task Can_Filter_MockAggregate()
    {
      var store = GetViewStore();

      var a1 = new MockAggregate
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
            MockString = "Hello!",
            MockDecimal = 12m,
            MockDouble = 0.22123
          },
          new ()
          {
            MockBoolean = true,
            MockString = "Baguette",
            MockDecimal = 32m,
            MockDouble = 0.123123
          },
        },
        
        MockFloatList = new List<float> { .1f, .5f, .9f },
        MockStringSet = new HashSet<string> { "A", "B", "C", "C" }
      };

      var a2 = new MockAggregate
      {
        MockBoolean = false,
        MockString = "Guten Tag",
        MockDecimal = -1,
        MockDouble = 3.14159,
        MockEnum = MockEnum.B,
        MockFlagEnum = MockFlagEnum.C | MockFlagEnum.E,
        
        MockNestedClass = new MockNestedClass
        {
          MockBoolean = true,
          MockString = "Buenos Dias",
          MockDecimal = .10m,
          MockDouble = 2.123123
        },
        
        MockNestedClassList = new List<MockNestedClass>
        {
          new ()
          {
            MockBoolean = false,
            MockString = "Bye Bye",
            MockDecimal = 23.231m,
            MockDouble = 0.123123
          },
          new ()
          {
            MockBoolean = false,
            MockString = "Croissant",
            MockDecimal = 6m,
            MockDouble = 0.111
          },
        },
        
        MockFloatList = new List<float> { 1f, 2f, 3f },
        MockStringSet = new HashSet<string> { "Just one item" }
      };

      await store.UpsertAsync(a1);
      await store.UpsertAsync(a2);

      var queryable = store
        .Query<MockAggregate, MockAggregateView>()
        .Where(x => x.Id == a1.Id || x.Id == a2.Id);

      // Can Filter By MockString
      var result1 = await queryable
        .Where(x => x.MockString == a1.MockString).ToListAsync();
      Assert.Equal(result1.Single().Id, a1.Id);

      // Can Filter By Nested Decimal
      var result2 = await queryable
        .Where(x => x.MockNestedClass.MockDecimal == a2.MockNestedClass.MockDecimal).ToListAsync();
      Assert.Equal(result2.Single().Id, a2.Id);
      
      // Can Filter On List Contents
      var result3 = await queryable
        .Where(x => x.MockFloatList.Contains(1f)).ToListAsync();
      Assert.Equal(result3.Single().Id, a2.Id);
      
      // Can Filter On Set Contents
      var result4 = await queryable
        .Where(x => x.MockStringSet.Contains("B")).ToListAsync();
      Assert.Equal(result4.Single().Id, a1.Id);
      
      // Can Filter on FlagEnum
      var result5 = await queryable
        .Where(x => (x.MockFlagEnum & MockFlagEnum.D) == MockFlagEnum.D).ToListAsync();
      Assert.Equal(result5.Single().Id, a1.Id);
      
      // Can Filter on Nested Class Attribute
      var result6 = await queryable
        .Where(x => x.MockNestedClassList.Select(y => y.MockString).Contains("Croissant"))
        .ToListAsync();
      Assert.Equal(result6.Single().Id, a2.Id);
    }
  }
}