using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public record EventEntity : Event { public string Json { get; set; } }

public record SnapshotEntity : Snapshot { public string Json { get; set; } }

public class RecordContext : DbContext
{
  public RecordContext() {}
  public RecordContext(DbContextOptions options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    var e = builder.Entity<EventEntity>();
    e.HasKey(x => new { x.PartitionId, x.AggregateId, x.Index });
    e.HasIndex(x => x.AggregateType);
    e.HasIndex(x => x.Type);
    e.HasIndex(x => x.Timestamp);
    
    var s = builder.Entity<SnapshotEntity>();
    s.HasKey(x => new { x.PartitionId, x.AggregateId, x.Index });
    s.HasIndex(x => x.AggregateType);
    s.HasIndex(x => x.Type);
    s.HasIndex(x => x.Timestamp);
  }
}