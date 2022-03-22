using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventSourcing.EF;

public static class ModelBuilderExtensions
{
  private const string PreviousIndex = "PreviousIndex";
  
  public static ModelBuilder SnapshotEntity(this ModelBuilder builder, Type aggregateType)
  {
    var eventType = typeof(Event<>).MakeGenericType(aggregateType);
    var snapshotType = typeof(Snapshot<>).MakeGenericType(aggregateType);
    return builder.Entity(snapshotType, x =>
    {
      x.ToTable(aggregateType.SnapshotTable());
      x.ConfigureRecord();

      x.HasKey(nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index));
      x.HasCheckConstraint($"CK_{aggregateType.SnapshotTable()}_NonNegativeIndex", $"\"{nameof(Snapshot.Index)}\" >= 0");

      // Tie Snapshot to corresponding Event, such that when the Event is removed, the Snapshot is also removed
      x.HasOne(eventType).WithOne()
        .HasForeignKey(snapshotType, nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index))
        .OnDelete(DeleteBehavior.Cascade);
    });
  }

  public static ModelBuilder EventEntity(this ModelBuilder builder, Type aggregateType)
  {
    var eventType = typeof(Event<>).MakeGenericType(aggregateType);
    return builder.Entity(eventType, x =>
    {
      x.ToTable(aggregateType.EventTable());
      x.ConfigureRecord();
      
      x.HasKey(nameof(Snapshot.PartitionId), nameof(Snapshot.AggregateId), nameof(Snapshot.Index));
      x.HasCheckConstraint($"CK_{aggregateType.EventTable()}_NonNegativeIndex", $"\"{nameof(Snapshot.Index)}\" >= 0");

      // Add Computed Shadow Property which contains the index of the previous event
      // PreviousIndex = NULL if Index == 0 else Index -1
      x.Property<long?>(PreviousIndex)
        .HasComputedColumnSql($"CASE WHEN \"{nameof(Event.Index)}\" = 0 THEN NULL ELSE \"{nameof(Event.Index)}\" - 1 END", true);
      
      // SQL Server does not supported filtered indices on computed columns:
      // When adding a foreign key for PreviousIndex, EF Core also adds a filtered unique index (since it's nullable)
      // The Index below is explicitly defined as not unique, which prevents EF Core from adding a filtered unique index 
      x.HasIndex(nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex).IsUnique(false);
      
      // Enforce Event consecutiveness by defining a foreign key to the previous event using the PreviousIndex column
      // Restrict deletion of Events that have events after them.
      // DELETE WHERE still works for deleting all events.
      x.HasOne(eventType).WithOne()
        .HasForeignKey(eventType, nameof(Event.PartitionId), nameof(Event.AggregateId), PreviousIndex)
        .HasConstraintName($"FK_{aggregateType.EventTable()}_ConsecutiveIndex")
        .OnDelete(DeleteBehavior.Restrict);
    });
  }

  public static ModelBuilder ProjectionEntity(this ModelBuilder builder, Type type)
  {
    EntityFrameworkRecordStore.ProjectionTypes.Add(type);
    return builder.Entity(type, x =>
    {
      x.ConfigureRecord();
      x.HasKey(nameof(Projection.PartitionId), nameof(Projection.AggregateId));

      x.HasIndex(nameof(Projection.Hash));
      x.Property(nameof(Projection.Hash)).HasMaxLength(IHashable.HashLength);
    });
  }

  private static void ConfigureRecord(this EntityTypeBuilder builder)
  {
    builder.HasIndex(nameof(Record.AggregateType));
    builder.HasIndex(nameof(Record.Type));
    builder.HasIndex(nameof(Record.Timestamp));
    
    builder.Property(nameof(Record.AggregateType)).HasMaxLength(RecordContext.MaxTypeLength);
    builder.Property(nameof(Record.Type)).HasMaxLength(RecordContext.MaxTypeLength);
  }
}