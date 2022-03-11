using EventSourcing.Core.Tests.Mocks;

namespace EventSourcing.Core.Tests;

public abstract partial class EventSourcingTests
{
  [Fact]
  public async Task Can_Add_Event_In_Transaction()
  {
    var e = new EmptyEvent { AggregateId = Guid.NewGuid(), AggregateType = "AggregateType" };

    await RecordStore.CreateTransaction()
      .AddEvents(new List<Event> { e })
      .CommitAsync();

    var count = await RecordStore.Events
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

    await RecordStore.CreateTransaction()
      .AddEvents(new List<Event> { e1 })
      .AddEvents(new List<Event> { e2 })
      .CommitAsync();

    var count = await RecordStore.Events
      .Where(x => x.AggregateId == e1.AggregateId || x.AggregateId == e2.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(2, count);
  }

  [Fact]
  public async Task Cannot_Add_Different_Partition_Key_In_Transaction()
  {
    var e = new EmptyEvent
    {
      PartitionId = Guid.NewGuid(),
      AggregateId = Guid.NewGuid()
    };

    var transaction = RecordStore.CreateTransaction(Guid.NewGuid());
    Assert.Throws<RecordValidationException>(() => transaction.AddEvents(new List<Event> { e }));
    await transaction.CommitAsync();
      
    // Ensure e was not committed
    var count = await RecordStore.Events
      .Where(x => x.PartitionId == e.PartitionId && x.AggregateId == e.AggregateId)
      .AsAsyncEnumerable()
      .CountAsync();
    
    Assert.Equal(0, count);
  }
}