using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;
using EventSourcing.Core.Tests.Mocks;
using Xunit;

namespace EventSourcing.Core.Tests
{
  public abstract class EventStoreTests
  {
    protected abstract IEventStore GetEventStore();
    protected abstract IEventStore<TBaseEvent> GetEventStore<TBaseEvent>() where TBaseEvent : Event, new();

    [Fact]
    public async Task Can_Add_Event()
    {
      var store = GetEventStore();
      await store.AddAsync(new Event[] { new EmptyAggregate().Add(new EmptyEvent()) });
    }

    [Fact]
    public async Task Can_Add_Multiple_Events()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var events = new List<Event>();

      for (var i = 0; i < 10; i++)
        events.Add(aggregate.Add(new EmptyEvent()));

      await store.AddAsync(events);
    }
    
    [Fact]
    public async Task Can_Add_Empty_Event_List()
    {
      var store = GetEventStore();
      await store.AddAsync(Array.Empty<Event>());
    }
    
    [Fact]
    public async Task Cannot_Add_Null_Event_List()
    {
      var store = GetEventStore();
      await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddAsync(null));
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e1 = aggregate.Add(new EmptyEvent());
      var e2 = aggregate.Add(new EmptyEvent());

      e2 = e2 with { AggregateVersion = 0 };

      await store.AddAsync(new Event[] { e1 });

      var exception = await Assert.ThrowsAnyAsync<ConcurrencyException>(
        async () => await store.AddAsync(new Event[] { e2 }));

      Assert.IsType<ConcurrencyException>(exception);
    }

    [Fact]
    public async Task Cannot_Add_Event_With_Duplicate_AggregateId_And_Version_In_Batch()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e1 = aggregate.Add(new EmptyEvent());
      var e2 = aggregate.Add(new EmptyEvent());
      
      e2 = e2 with { AggregateVersion = 0 };

      var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
        async () => await store.AddAsync(new Event[] { e1, e2 }));
    }

    [Fact]
    public async Task Cannot_Add_Events_With_Different_AggregateIds_In_Batch()
    {
      var store = GetEventStore();

      var aggregate1 = new EmptyAggregate();
      var event1 = aggregate1.Add(new EmptyEvent());

      var aggregate2 = new EmptyAggregate();
      var event2 = aggregate2.Add(new EmptyEvent());

      await Assert.ThrowsAnyAsync<ArgumentException>(
        async () => await store.AddAsync(new Event[] { event1, event2 }));
    }
    
    [Fact]
    public async Task Cannot_Add_NonConsecutive_Events()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e1 = aggregate.Add(new EmptyEvent());
      await store.AddAsync(new List<Event> { e1 });
      
      var e2 = aggregate.Add(new EmptyEvent()) with { AggregateVersion = 2 };
      
      await Assert.ThrowsAnyAsync<InvalidOperationException>(
        async () => await store.AddAsync(new Event[] { e2 }));
    }
    
    [Fact]
    public async Task Cannot_Add_NonConsecutive_Events_In_Batch()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e1 = aggregate.Add(new EmptyEvent());
      var e2 = aggregate.Add(new EmptyEvent()) with { AggregateVersion = 2 };
      
      await Assert.ThrowsAnyAsync<InvalidOperationException>(
        async () => await store.AddAsync(new Event[] { e1, e2 }));
    }
    
    [Fact]
    public async Task Adding_Event_With_Duplicate_Version_Throws_Exception_With_Duplicate_Version_Message()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e1 = aggregate.Add(new EmptyEvent());
      var e2 = aggregate.Add(new EmptyEvent());

      e2 = e2 with { AggregateVersion = 0 };

      await store.AddAsync(new Event[] { e1 });

      var exception = await Assert.ThrowsAnyAsync<ConcurrencyException>(
        async () => await store.AddAsync(new Event[] { e2 }));

      Assert.IsType<ConcurrencyException>(exception);
      Assert.Contains(new ConcurrencyException(e2).Message, exception.Message);
    }

    [Fact]
    public async Task Can_Get_Events_By_AggregateId()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      await store.AddAsync(events);

      var result = await store.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .ToListAsync();

      Assert.Equal(events.Count, result.Count);
    }

    [Fact]
    public async Task Can_Filter_Events_By_Aggregate_Version()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      var aggregate2 = new EmptyAggregate();
      var events2 = new List<Event>
      {
        aggregate2.Add(new EmptyEvent()),
        aggregate2.Add(new EmptyEvent())
      };

      await store.AddAsync(events);
      await store.AddAsync(events2);

      var result = await store.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .Where(x => x.AggregateVersion > 0)
        .ToListAsync();

      Assert.Single(result);
    }

    [Fact]
    public async Task Can_Filter_Events_By_Aggregate_Type()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var events = new List<Event>
      {
        aggregate.Add(new EmptyEvent()),
        aggregate.Add(new EmptyEvent())
      };

      var aggregate2 = new SimpleAggregate();
      var events2 = new List<Event>
      {
        aggregate2.Add(new EmptyEvent()),
        aggregate2.Add(new EmptyEvent())
      };

      await store.AddAsync(events);
      await store.AddAsync(events2);

      // If I just would query all events by Type, I'd get all events from the history of tests
      // Therefore I test the same thing by two assertions.

      var result = await store.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .Where(x => x.AggregateType == aggregate.Type)
        .ToListAsync();

      Assert.All(result, e => Assert.Equal(aggregate.Id, e.AggregateId));

      var result2 = await store.Events
        .Where(x => x.AggregateId == aggregate2.Id)
        .Where(x => x.AggregateType == aggregate.Type)
        .ToListAsync();

      Assert.Empty(result2);
    }

    [Fact]
    public async Task Can_Add_Mock_Event()
    {
      var store = GetEventStore();

      var aggregate = new EmptyAggregate();
      var e = aggregate.Add(new MockEvent
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

      await store.AddAsync(new List<Event> { e });

      var result = (await store.Events
          .Where(x => x.AggregateId == aggregate.Id)
          .ToListAsync())
        .Cast<MockEvent>()
        .Single();
      
      Assert.Equal(e.MockBoolean, result.MockBoolean);
      Assert.Equal(e.MockString, result.MockString);
      Assert.Equal(e.MockDecimal, result.MockDecimal);
      Assert.Equal(e.MockDouble, result.MockDouble);
      Assert.Equal(e.MockEnum, result.MockEnum);
      Assert.Equal(e.MockFlagEnum, result.MockFlagEnum);
      Assert.Equal(e.MockNestedClass.MockBoolean, result.MockNestedClass.MockBoolean);
      Assert.Equal(e.MockNestedClass.MockString, result.MockNestedClass.MockString);
      Assert.Equal(e.MockNestedClass.MockDecimal, result.MockNestedClass.MockDecimal);
      Assert.Equal(e.MockNestedClass.MockDouble, result.MockNestedClass.MockDouble);
      Assert.Equal(e.MockNestedClassList.Single().MockBoolean, result.MockNestedClassList.Single().MockBoolean);
      Assert.Equal(e.MockNestedClassList.Single().MockString, result.MockNestedClassList.Single().MockString);
      Assert.Equal(e.MockNestedClassList.Single().MockDecimal, result.MockNestedClassList.Single().MockDecimal);
      Assert.Equal(e.MockNestedClassList.Single().MockDouble, result.MockNestedClassList.Single().MockDouble);
      Assert.Equal(e.MockFloatList[0], result.MockFloatList[0]);
      Assert.Equal(e.MockFloatList[1], result.MockFloatList[1]);
      Assert.Equal(e.MockFloatList[2], result.MockFloatList[2]);
      Assert.Contains(result.MockStringSet, x => x == "A");
      Assert.Contains(result.MockStringSet, x => x == "B");
      Assert.Contains(result.MockStringSet, x => x == "C");
    }
    
    [Fact]
    public async Task Can_Filter_On_Mock_Base_Event()
    {
      var store = GetEventStore<MockEvent>();

      var aggregate = new EmptyAggregate();
      
      var e1 = aggregate.Add(new MockEvent
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
      });

      var e2 = aggregate.Add(new MockEvent
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
      });

      await store.AddAsync(new List<MockEvent> { e1, e2 });

      var queryable = store.Events.Where(x => x.AggregateId == aggregate.Id);

      // Can Filter By MockString
      var result1 = await queryable
        .Where(x => x.MockString == e1.MockString).ToListAsync();
      Assert.Equal(result1.Single().EventId, e1.EventId);

      // Can Filter By Nested Decimal
      var result2 = await queryable
        .Where(x => x.MockNestedClass.MockDecimal == e2.MockNestedClass.MockDecimal).ToListAsync();
      Assert.Equal(result2.Single().EventId, e2.EventId);
      
      // Can Filter On List Contents
      var result3 = await queryable
        .Where(x => x.MockFloatList.Contains(1f)).ToListAsync();
      Assert.Equal(result3.Single().EventId, e2.EventId);
      
      // Can Filter On Set Contents
      var result4 = await queryable
        .Where(x => x.MockStringSet.Contains("B")).ToListAsync();
      Assert.Equal(result4.Single().EventId, e1.EventId);
      
      // Can Filter on FlagEnum
      var result5 = await queryable
        .Where(x => (x.MockFlagEnum & MockFlagEnum.D) == MockFlagEnum.D).ToListAsync();
      Assert.Equal(result5.Single().EventId, e1.EventId);
      
      // Can Filter on Nested Class Attribute
      var result6 = await queryable
        .Where(x => x.MockNestedClassList.Select(y => y.MockString).Contains("Croissant"))
        .ToListAsync();
      Assert.Equal(result6.Single().EventId, e2.EventId);
    }
  }
}