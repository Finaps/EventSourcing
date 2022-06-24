using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Finaps.EventSourcing.EF;

/// <summary>
/// Finaps.EventSourcing specific extensions for EF Core ModelBuilder
/// </summary>
public static class ModelBuilderExtensions
{
  public static EventCollectionBuilder<TAggregate> EventCollection<TAggregate>(this ModelBuilder builder)
    where TAggregate : Aggregate, new() => new(builder);

  public static SnapshotCollectionBuilder<TAggregate> SnapshotCollection<TAggregate>(this ModelBuilder builder)
    where TAggregate : Aggregate, new() => new(builder);

  public static EntityTypeBuilder<TProjection> ProjectionEntity<TProjection>(this ModelBuilder builder)
    where TProjection : Projection
  {
    var projection = builder.Entity<TProjection>();
    
    // Generic Record Configuration
    projection.HasRecordIndices();

    // Projections are uniquely identified by their PartitionId and AggregateId
    projection.HasKey(x => new { x.PartitionId, x.AggregateId });

    // To quickly find Projections with an outdated hash
    projection.HasIndex(x => x.Hash);

    // Hashes have a fixed length, better capitalize on that knowledge
    projection.Property(nameof(Projection.Hash)).HasMaxLength(IHashable.HashLength);

    return projection;
  }
}