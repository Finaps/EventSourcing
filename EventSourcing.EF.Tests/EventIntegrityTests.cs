using System;
using System.Linq;
using System.Threading.Tasks;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finaps.EventSourcing.EF.Tests;

public abstract partial class EntityFrameworkEventSourcingTests
{
  [Fact]
  public async Task EventIntegrityTests_Can_Insert_Event()
  {
    var context = GetRecordContext();
    
    var entry = context.Add(new SimpleEvent
    {
      AggregateId = Guid.NewGuid(),
      AggregateType = nameof(SimpleAggregate)
    });

    await context.SaveChangesAsync();

    Assert.True(await context.Set<SimpleEvent>().AnyAsync(x => x.AggregateId == entry.Entity.AggregateId));
  }
  
  [Fact]
  public async Task EventIntegrityTests_Can_Delete_Last_Event()
  {
    var aggregate = new SimpleAggregate();

    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new SimpleEvent()))
      .ToList();
    
    await GetAggregateService().PersistAsync(aggregate);
    
    var context = GetRecordContext();
    
    Assert.True(await context.Set<SimpleEvent>().AnyAsync(x => x.AggregateId == aggregate.Id && x.Index == 9));

    context.Remove(events.Last());
    await context.SaveChangesAsync();
    
    Assert.False(await context.Set<SimpleEvent>().AnyAsync(x => x.AggregateId == aggregate.Id && x.Index == 9));
  }

  [Fact]
  public async Task EventIntegrityTests_Cannot_Insert_Event_With_Null_AggregateType()
  {
    var context = GetRecordContext();
    
    context.Add(new SimpleEvent
    {
      AggregateId = Guid.NewGuid(),
      AggregateType = null
    });

    await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
  }

  [Fact]
  public async Task EventIntegrityTests_Cannot_Insert_Nonconsecutive_Event()
  {
    var context = GetRecordContext();
    
    context.Add(new SimpleEvent
    {
      AggregateId = Guid.NewGuid(),
      AggregateType = nameof(SimpleAggregate),
      Index = 99
    });

    await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
  }
  
  [Fact]
  public async Task EventIntegrityTests_Cannot_Delete_Nonconsecutive_Event()
  {
    var aggregate = new SimpleAggregate();

    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new SimpleEvent()))
      .ToList();
    
    await GetAggregateService().PersistAsync(aggregate);

    var context = GetRecordContext();

    context.Remove(events[5]);
    await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
  }

  [Fact]
  public async Task EventIntegrityTests_Cannot_Create_Event_With_Negative_Index()
  {
    var context = GetRecordContext();
    
    context.Add(new SimpleEvent
    {
      AggregateId = Guid.NewGuid(),
      AggregateType = nameof(SimpleAggregate),
      Index = -1
    });

    await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
  }
  
  [Fact]
  public async Task EventIntegrityTests_Deleting_Event_Deletes_Snapshot()
  {
    var aggregate = new SnapshotAggregate();

    var events = Enumerable
      .Range(0, 10)
      .Select(_ => aggregate.Apply(new SnapshotEvent()))
      .ToList();
    
    await GetAggregateService().PersistAsync(aggregate);

    var context = GetRecordContext();

    Assert.True(await context.Set<SnapshotSnapshot>().AnyAsync(x => x.AggregateId == aggregate.Id));

    context.Remove(events.Last());
    await context.SaveChangesAsync();
    
    Assert.False(await context.Set<SnapshotSnapshot>().AnyAsync(x => x.AggregateId == aggregate.Id));
  }

  [Fact]
  public async Task EventIntegrityTests_Cannot_Add_Snapshot_Without_Corresponding_Event()
  {
    var snapshot = new SnapshotSnapshot { AggregateId = Guid.NewGuid(), Index = 0 };
    await Assert.ThrowsAsync<RecordStoreException>(async () => await GetRecordStore().AddSnapshotAsync(snapshot));
  }
}