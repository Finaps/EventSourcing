using EventSourcing.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventSourcing.EF;

public static class ModelBuilderExtensions
{
  public static EntityTypeBuilder<TEvent> EventEntity<TEvent>(this ModelBuilder builder) where TEvent : Event
  {
    var eventBuilder = builder.Entity<TEvent>();
    eventBuilder.HasKey(x => new { x.PartitionId, x.AggregateId, x.Index });
    eventBuilder.HasIndex(x => x.AggregateType);
    eventBuilder.HasIndex(x => x.Type);
    eventBuilder.HasIndex(x => x.Timestamp);
    return eventBuilder;
  }
  
  public static EntityTypeBuilder<TProjection> ProjectionEntity<TProjection>(this ModelBuilder builder) where TProjection : Projection
  {
    EntityFrameworkRecordStore.ProjectionTypes.Add(typeof(TProjection));
    var projectionBuilder = builder.Entity<TProjection>();
    projectionBuilder.HasKey(x => new { x.PartitionId, x.AggregateId });
    projectionBuilder.HasIndex(x => x.AggregateType);
    projectionBuilder.HasIndex(x => x.Hash);
    return projectionBuilder;
  }
}