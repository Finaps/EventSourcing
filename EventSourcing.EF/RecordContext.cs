using System.Text.Json;
using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public record EventEntity : Event { public JsonDocument Json { get; set; } }

public record SnapshotEntity : Snapshot { public JsonDocument Json { get; set; } }

public class RecordContext : DbContext
{
  public RecordContext() {}
  public RecordContext(DbContextOptions options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.EventEntity<EventEntity>();
    builder.EventEntity<SnapshotEntity>();
  }
}