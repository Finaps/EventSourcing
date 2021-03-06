namespace Finaps.EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task AggregateService_RehydrateAndPersist_Can_Rehydrate_And_Persist()
  {
    var aggregate = new SimpleAggregate();
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
    };

    await GetAggregateService().PersistAsync(aggregate);
    await GetAggregateService().RehydrateAndPersistAsync<SimpleAggregate>(aggregate.Id,
      a => a.Apply(new SimpleEvent()));

    var count = await GetRecordStore()
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }

  [Fact]
  public async Task AggregateService_RehydrateAndPersist_Can_Rehydrate_And_Persist_MockAggregate()
  {
    var aggregate = new MockAggregate();
    var e = aggregate.Apply(new MockEvent
    {
      MockBoolean = true,
      MockString = "Hello World",
      MockNullableString = null,
      MockDecimal = 0.55m,
      MockDouble = 0.6,
      MockNullableDouble = null,
      MockEnum = MockEnum.C,
      MockFlagEnum = MockFlagEnum.A | MockFlagEnum.D,

      MockNestedRecord = new MockNestedRecord(false, "Bonjour", .82m, .999),

      MockNestedRecordList = new List<MockNestedRecordItem>
      {
        new(false, "Bye Bye", 99.99m, 0.111)
      },

      MockFloatList = new List<float> { .1f, .5f, .9f },
      MockStringSet = new List<string> { "A", "B", "C", "C" }
    });

    await GetAggregateService().PersistAsync(aggregate);

    var result = await GetAggregateService().RehydrateAsync<MockAggregate>(aggregate.Id);

    IMock.AssertEqual(e, result!);
  }

  [Fact]
  public async Task AggregateService_RehydrateAndPersist_Can_Rehydrate_And_Persist_With_PartitionId()
  {
    var aggregate = new SimpleAggregate { PartitionId = Guid.NewGuid() };
    var events = new List<Event>
    {
      aggregate.Apply(new SimpleEvent()),
      aggregate.Apply(new SimpleEvent()),
    };

    await GetAggregateService().PersistAsync(aggregate);
    await GetAggregateService().RehydrateAndPersistAsync<SimpleAggregate>(aggregate.PartitionId, aggregate.Id,
      a => a.Apply(new SimpleEvent()));

    var count = await GetRecordStore()
      .GetEvents<SimpleAggregate>()
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    Assert.Equal(events.Count + 1, count);
  }
}