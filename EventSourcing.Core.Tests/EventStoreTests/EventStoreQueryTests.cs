using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
    [Fact]
  public async Task Can_Query_Events_By_PartitionId()
  {
    var aggregate1 = new EmptyAggregate { PartitionId = Guid.NewGuid() };
    var events1 = new List<Event>
    {
      aggregate1.Add(new EmptyEvent()),
      aggregate1.Add(new EmptyEvent()),
      aggregate1.Add(new EmptyEvent()),
      aggregate1.Add(new EmptyEvent())
    };

    await EventStore.AddAsync(events1);

    var count = await EventStore.Events
      .Where(x => x.PartitionId == aggregate1.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events1.Count, count);
  }
  
  [Fact]
  public async Task Can_Query_Events_By_AggregateId()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>
    {
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent()),
      aggregate.Add(new EmptyEvent())
    };

    await EventStore.AddAsync(events);

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count, count);
  }

  [Fact]
  public async Task Can_Query_Events_By_AggregateVersion()
  {
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

    await EventStore.AddAsync(events);
    await EventStore.AddAsync(events2);

    var result = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .Where(x => x.Index > 0)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.Single(result);
  }

  [Fact]
  public async Task Can_Query_Events_By_AggregateType()
  {
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

    await EventStore.AddAsync(events);
    await EventStore.AddAsync(events2);

    // If I just would query all events by Type, I'd get all events from the history of tests
    // Therefore I test the same thing by two assertions.

    var result = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .Where(x => x.AggregateType == aggregate.Type)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.All(result, e => Assert.Equal(aggregate.Id, e.AggregateId));

    var result2 = await EventStore.Events
      .Where(x => x.AggregateId == aggregate2.Id)
      .Where(x => x.AggregateType == aggregate.Type)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.Empty(result2);
  }

  [Fact]
  public async Task Can_Query_Mock_Event()
  {
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
        new()
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

    await EventStore.AddAsync(new List<Event> { e });

    var result = (await EventStore.Events
        .Where(x => x.AggregateId == aggregate.Id)
        .AsAsyncEnumerable()
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
    Assert.Contains(e.MockStringSet, x => x == "A");
    Assert.Contains(e.MockStringSet, x => x == "B");
    Assert.Contains(e.MockStringSet, x => x == "C");
  }
}