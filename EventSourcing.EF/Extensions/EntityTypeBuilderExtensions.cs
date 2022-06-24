using Finaps.EventSourcing.Core;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finaps.EventSourcing.EF;

internal static class EntityTypeBuilderExtensions
{
  public static EntityTypeBuilder HasRecordIndices(this EntityTypeBuilder builder)
  {
    builder.Property(nameof(Record.AggregateType)).IsRequired();
    builder.HasIndex(nameof(Record.AggregateType));
    
    builder.Property(nameof(Record.Type)).IsRequired();
    builder.HasIndex(nameof(Record.Type));
    
    builder.Property(nameof(Record.Timestamp)).IsRequired();
    builder.HasIndex(nameof(Record.Timestamp));
    
    return builder;
  }
}