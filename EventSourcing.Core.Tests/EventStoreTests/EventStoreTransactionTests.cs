using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventStoreTests
{
  [Fact]
  public async Task Can_Add_Event_In_Transaction()
  {
    var e = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };

    await EventStore.CreateTransaction()
      .Add(new List<Event> { e })
      .CommitAsync();

    var count = await EventStore.Events
      .Where(x => x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(1, count);
  }
  
  [Fact]
  public async Task Can_Add_Multiple_Events_In_Transaction()
  {
    var e1 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };
    var e2 = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };

    await EventStore.CreateTransaction()
      .Add(new List<Event> { e1 })
      .Add(new List<Event> { e2 })
      .CommitAsync();

    var count = await EventStore.Events
      .Where(x => x.AggregateId == e1.AggregateId || x.AggregateId == e2.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(2, count);
  }

  [Fact]
  public async Task Can_Delete_Events_In_Transaction()
  {
    var aggregate = new EmptyAggregate();
    await EventStore.AddAsync(Enumerable.Range(0, 5)
      .Select(_ => aggregate.Add(new Event()))
      .ToList());

    await EventStore.CreateTransaction()
      .Delete(aggregate.Id, aggregate.Version)
      .CommitAsync();

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
  
  [Fact]
  public async Task Can_Add_And_Delete_Events_In_Same_Transaction()
  {
    var aggregate = new EmptyAggregate();
    await EventStore.AddAsync(Enumerable.Range(0, 5)
      .Select(_ => aggregate.Add(new Event()))
      .ToList());

    var aggregate2 = new EmptyAggregate();

    await EventStore.CreateTransaction()
      .Add(Enumerable.Range(0, 5).Select(_ => aggregate2.Add(new Event())).ToList())
      .Delete(aggregate.Id, aggregate.Version)
      .CommitAsync();

    var count = await EventStore.Events
      .Where(x => x.AggregateId == aggregate.Id)
      .AsAsyncEnumerable()
      .CountAsync();

    var count2 = await EventStore.Events
      .Where(x => x.AggregateId == aggregate2.Id)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
    Assert.Equal(5, count2);
  }

  [Fact]
  public async Task Cannot_Add_Different_Partition_Key_In_Transaction()
  {
    var e = new EmptyEvent
    {
      PartitionId = Guid.NewGuid(),
      AggregateId = Guid.NewGuid()
    };

    var transaction = EventStore.CreateTransaction(Guid.NewGuid());
    Assert.Throws<RecordValidationException>(() => transaction.Add(new List<Event> { e }));
    await transaction.CommitAsync();
      
    // Ensure e was not committed
    var count = await EventStore.Events
      .Where(x => x.PartitionId == e.PartitionId && x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
}