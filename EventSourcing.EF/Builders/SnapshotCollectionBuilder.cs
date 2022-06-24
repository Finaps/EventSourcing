using System.Linq.Expressions;
using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finaps.EventSourcing.EF;

public class SnapshotCollectionBuilder<TAggregate> where TAggregate : Aggregate, new()
{
  public readonly string TableName = $"{typeof(TAggregate).Name}{nameof(Snapshot)}";
  
  private readonly ModelBuilder _modelBuilder;
  private readonly EntityTypeBuilder<Snapshot<TAggregate>> _snapshotBuilder;
  
  public SnapshotCollectionBuilder(ModelBuilder builder)
  {
    _modelBuilder = builder;
    _snapshotBuilder = builder.Entity<Snapshot<TAggregate>>().ToTable(TableName);

    // Generic Record Configuration
    _snapshotBuilder.HasRecordIndices();

    // Add table for the Snapshot<TAggregate> hierarchy
    // See https://docs.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-hierarchy-and-discriminator-configuration
    _snapshotBuilder.HasKey(x => new { x.PartitionId, x.AggregateId, x.Index });
    
    EnforceIndexNonNegativeness();
    EnforceCorrespondingEvent();
  }
  
  public SnapshotCollectionBuilder<TAggregate> Add<TEvent>() where TEvent : Event<TAggregate>
  {
    _modelBuilder.Entity<TEvent>().HasDiscriminator(x => x.Type);

    return this;
  }

  public ReferenceCollectionBuilder<Snapshot<TForeignAggregate>, TSnapshot> AggregateReference<TSnapshot, TForeignAggregate>(
    Expression<Func<TSnapshot, Guid?>> navigation) where TSnapshot : Snapshot<TAggregate> where TForeignAggregate : Aggregate, new()
  {
    const string ZeroIndex = "ZeroIndex";
    
    var foreignAggregateId = navigation.GetSimpleMemberName();
    var foreignKeyName = $"FK_{typeof(TSnapshot).Name}_{foreignAggregateId}";
    
    // Add column of all zeros
    _snapshotBuilder.Property<long>(ZeroIndex).HasComputedColumnSql("cast(0 as bigint)", true);

    return _modelBuilder
      // Add one to many relation between Snapshot<TForeignAggregate> and TSnapshot
      .Entity<TSnapshot>()
      .HasOne<Snapshot<TForeignAggregate>>()
      .WithMany()

      // The foreign key { PartitionId, ForeignAggregateId, ZeroIndex } in TSnapshot
      // points to the Principal key { PartitionId, AggregateId, Index } in Snapshot<TForeignAggregate>
      .HasForeignKey(nameof(Snapshot.PartitionId), foreignAggregateId, ZeroIndex)
      .HasConstraintName(foreignKeyName)
      
      // Restrict deletion of Events by default
      .OnDelete(DeleteBehavior.Restrict)
      .IsRequired();
  }

  private void EnforceCorrespondingEvent()
  {
    // Add Foreign Key to tie Snapshot to corresponding Event
    // When the Event is removed, the Snapshot is also removed
    _snapshotBuilder
      .HasOne<Event<TAggregate>>()
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);
  }

  private void EnforceIndexNonNegativeness()
  {
    _snapshotBuilder.HasCheckConstraint($"CK_{TableName}_NonNegativeIndex", $"\"{nameof(Snapshot.Index)}\" >= 0");
  }
}