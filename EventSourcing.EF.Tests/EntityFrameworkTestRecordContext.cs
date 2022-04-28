using Finaps.EventSourcing.Core.Tests.Mocks;
using Finaps.EventSourcing.EF.Tests.Mocks;
using Microsoft.EntityFrameworkCore;

namespace Finaps.EventSourcing.EF.Tests;

public class EntityFrameworkTestRecordContext : RecordContext
{
  public EntityFrameworkTestRecordContext(DbContextOptions options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<MockEvent>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });

    builder.Entity<MockSnapshot>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });
    
    builder.Entity<MockAggregateProjection>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });

    builder.AggregateReference<ReferenceEvent, ReferenceAggregate>(x => x.ReferenceAggregateId);
    builder.AggregateReference<ReferenceEvent, EmptyAggregate>(x => x.EmptyAggregateId);
  }
}