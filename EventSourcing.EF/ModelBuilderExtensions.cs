using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EF;

public static class ModelBuilderExtensions
{
  public static ModelBuilder SnapshotEntity(this ModelBuilder builder, Type aggregateType) =>
    builder.Entity(typeof(Snapshot<>).MakeGenericType(aggregateType), x =>
    {
      x.ToTable(aggregateType.SnapshotTable());
      x.HasKey(nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index));
      x.HasIndex(nameof(Snapshot.Type));
      x.HasIndex(nameof(Snapshot.Timestamp));
      x.Property(nameof(Snapshot.Type)).HasMaxLength(256);
      x.Property(nameof(Snapshot.AggregateType)).HasMaxLength(256);
    });
  
  public static ModelBuilder EventEntity(this ModelBuilder builder, Type aggregateType) =>
    builder.Entity(typeof(Event<>).MakeGenericType(aggregateType), x =>
    {
      x.ToTable(aggregateType.EventTable());
      x.HasKey(nameof(Event.PartitionId), nameof(Event.AggregateId), nameof(Event.Index));
      x.HasIndex(nameof(Event.Type));
      x.HasIndex(nameof(Event.Timestamp));
      x.Property(nameof(Event.Type)).HasMaxLength(256);
      x.Property(nameof(Event.AggregateType)).HasMaxLength(256);
    });

  public static ModelBuilder ProjectionEntity(this ModelBuilder builder, Type type)
  {
    EntityFrameworkRecordStore.ProjectionTypes.Add(type);
    return builder.Entity(type, x =>
    {
      x.HasKey(nameof(Projection.PartitionId), nameof(Projection.AggregateId));
      x.HasIndex(nameof(Projection.AggregateType));
      x.HasIndex(nameof(Projection.Hash));

      x.Property(nameof(Projection.Type)).HasMaxLength(256);
      x.Property(nameof(Projection.AggregateType)).HasMaxLength(256);
      x.Property(nameof(Projection.Hash)).HasMaxLength(256);
    });
  }
}