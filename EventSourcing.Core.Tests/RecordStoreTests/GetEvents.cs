namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_PartitionId()
  {
    var aggregate1 = new EmptyAggregate { PartitionId = Guid.NewGuid() };
    var events1 = new []
    {
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent()),
      aggregate1.Apply(new EmptyEvent())
    };

    await GetRecordStore().AddEventsAsync(events1);

    var count = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.PartitionId == aggregate1.PartitionId)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events1.Length, count);
  }
  
  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_AggregateId()
  {
    var aggregate = new EmptyAggregate();
    var events = new []
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    await GetRecordStore().AddEventsAsync(events);

    var count = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Length, count);
  }

  [Fact]
  public async Task RecordStore_GetEvents_Can_Query_Events_By_AggregateVersion()
  {
    var aggregate = new EmptyAggregate();
    var events = new []
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    var aggregate2 = new EmptyAggregate();
    var events2 = new []
    {
      aggregate2.Apply(new EmptyEvent()),
      aggregate2.Apply(new EmptyEvent())
    };

    await GetRecordStore().AddEventsAsync(events);
    await GetRecordStore().AddEventsAsync(events2);

    var result = await GetRecordStore()
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
    var events = new []
    {
      aggregate.Apply(new EmptyEvent()),
      aggregate.Apply(new EmptyEvent())
    };

    var aggregate2 = new SimpleAggregate();
    var events2 = new []
    {
      aggregate2.Apply(new SimpleEvent()),
      aggregate2.Apply(new SimpleEvent())
    };

    await GetRecordStore().AddEventsAsync(events);
    await GetRecordStore().AddEventsAsync(events2);

    // If I just would query all events by Type, I'd get all events from the history of tests
    // Therefore I test the same thing by two assertions.

    var result = await GetRecordStore()
      .GetEvents<EmptyAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .ToListAsync();

    Assert.All(result, e => Assert.Equal(aggregate.Id, e.AggregateId));

    var result2 = await GetRecordStore()
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

    await GetRecordStore().AddEventsAsync(new [] { e });

    var result = (await GetRecordStore()
        .GetEvents<MockAggregate>()
        .Where(x => x.AggregateId == aggregate.Id)
        .AsAsyncEnumerable()
        .ToListAsync())
      .Cast<MockEvent>()
      .Single();
    
    IMock.AssertEqual(e, result);
  }
}