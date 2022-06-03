namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_PartitionId()
  {
    var aggregate1 = new EmptyAggregate { PartitionId = Guid.NewGuid() };
    var events1 = new List<Event>
    {
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent())
    };

    await RecordStore.AddEventsAsync(events1);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.PartitionId == aggregate1.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events1.Count, count);
  }
  
  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_AggregateId()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    await RecordStore.AddEventsAsync(events);

    var count = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count, count);
  }

  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_AggregateVersion()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    var aggregate2 = new EmptyAggregate();
    var events2 = new List<Event>
    {
      aggregate2.Apply(new EmptyEvent()),
      aggregate2.Apply(new EmptyEvent())
    };

    await RecordStore.AddEventsAsync(events);
    await RecordStore.AddEventsAsync(events2);

    var result = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .Where(x => x.Index > 0)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.Single(result);
  }

  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_AggregateType()
  {
    var aggregate = new EmptyAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    var aggregate2 = new SimpleAggregate();
    var events2 = new List<Event>
    {
      aggregate2.Apply(new SimpleEvent()),
      aggregate2.Apply(new SimpleEvent())
    };

    await RecordStore.AddEventsAsync(events);
    await RecordStore.AddEventsAsync(events2);

    // If I just would query all events by Type, I'd get all events from the history of tests
    // Therefore I test the same thing by two assertions.

    var result = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.All(result, e => Assert.Equal(aggregate.Id, e.AggregateId));

    var result2 = await RecordStore
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate2.Id)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.Empty(result2);
  }

  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Mock_Event()
  {
    var aggregate = new MockAggregate();
    var e = aggregate.Apply(new MockEvent
    {
      MockBoolean = true,
      MockString = "Hello World",
      MockDecimal = 0.55m,
      MockDouble = 0.6,
      MockEnum = MockEnum.C,
      MockFlagEnum = MockFlagEnum.A | MockFlagEnum.D,

      MockNestedRecord = new MockNestedRecord(false, "Bonjour", .82m, .999),

      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new(false, "Bye Bye", 99.99m, .111)
      },

      MockFloatList = new List<float> { .1f, .5f, .9f },
      MockStringSet = new List<string> { "A", "B", "C", "C" }
    });

    await RecordStore.AddEventsAsync(new List<Event> { e });

    var result = (await RecordStore
        .GetEvents<MockAggregate>()
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
    Assert.Equal(e.MockNestedRecord.MockBoolean, result.MockNestedRecord.MockBoolean);
    Assert.Equal(e.MockNestedRecord.MockString, result.MockNestedRecord.MockString);
    Assert.Equal(e.MockNestedRecord.MockDecimal, result.MockNestedRecord.MockDecimal);
    Assert.Equal(e.MockNestedRecord.MockDouble, result.MockNestedRecord.MockDouble);
    Assert.Equal(e.MockNestedRecordList.Single().MockBoolean, result.MockNestedRecordList.Single().MockBoolean);
    Assert.Equal(e.MockNestedRecordList.Single().MockString, result.MockNestedRecordList.Single().MockString);
    Assert.Equal(e.MockNestedRecordList.Single().MockDecimal, result.MockNestedRecordList.Single().MockDecimal);
    Assert.Equal(e.MockNestedRecordList.Single().MockDouble, result.MockNestedRecordList.Single().MockDouble);
    Assert.Equal(e.MockFloatList[0], result.MockFloatList[0]);
    Assert.Equal(e.MockFloatList[1], result.MockFloatList[1]);
    Assert.Equal(e.MockFloatList[2], result.MockFloatList[2]);
    Assert.Contains(e.MockStringSet, x => x == "A");
    Assert.Contains(e.MockStringSet, x => x == "B");
    Assert.Contains(e.MockStringSet, x => x == "C");
  }
}