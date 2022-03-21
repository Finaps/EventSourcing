using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventSourcing.EF.Tests.SqlServer;

public static class PropertyBuilderExtensions
{
  public static PropertyBuilder<List<float>> HasBinaryConversion(this PropertyBuilder<List<float>> builder) => builder
    .HasConversion(
      list => list.SelectMany(BitConverter.GetBytes).ToArray(),
      bytes => bytes.Chunk(4).Select(b => BitConverter.ToSingle(b)).ToList(),
      new ValueComparer<List<float>>((x, y) => x.SequenceEqual(y), x => x.GetHashCode()));
  
  public static PropertyBuilder<List<string>> HasStringConversion(this PropertyBuilder<List<string>> builder, string separator) => builder
    .HasConversion(
      list => string.Join(";", list),
      str => str.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList());
}