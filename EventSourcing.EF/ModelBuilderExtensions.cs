using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventSourcing.EF;

public static class ModelBuilderExtensions
{
  public static EntityTypeBuilder<TProjection> ProjectionEntity<TProjection>(this ModelBuilder builder) where TProjection : Projection
  {
    EntityFrameworkRecordStore.ProjectionTypes.Add(typeof(TProjection));
    var b = builder.Entity<TProjection>();
    b.HasKey(x => new { x.PartitionId, x.AggregateId });
    b.HasIndex(x => x.AggregateType);
    b.HasIndex(x => x.Hash);
    return b;
  }
}