using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EventSourcing.Core.Tests;
using EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EventSourcing.EF.Tests.SqlServer;

public class SqlServerTestContext : RecordContext
{
  public SqlServerTestContext(DbContextOptions<SqlServerTestContext> options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<EventEntity>()
      .Property(x => x.Json)
      .HasConversion(
        json => json.RootElement.ToString(), 
        str => JsonDocument.Parse(str, new JsonDocumentOptions()));
    
    builder.Entity<SnapshotEntity>()
      .Property(x => x.Json)
      .HasConversion(
        json => json.RootElement.ToString(), 
        str => JsonDocument.Parse(str, new JsonDocumentOptions()));

    var mockProjection = builder.ProjectionEntity<MockAggregateProjection>();
    mockProjection.OwnsOne(x => x.MockNestedRecord);
    mockProjection.OwnsMany(x => x.MockNestedRecordList);

    mockProjection
      .Property(x => x.MockFloatList)
      .HasConversion(
        list => list.SelectMany(BitConverter.GetBytes).ToArray(),
        bytes => bytes.Chunk(4).Select(b => BitConverter.ToSingle(b)).ToList(),
        new ValueComparer<List<float>>(
          (x, y) => x.SequenceEqual(y), x => x.GetHashCode()));
    
    mockProjection
      .Property(x => x.MockStringSet)
      .HasConversion(
        list => string.Join(";", list),
        str => str.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList());

    builder.ProjectionEntity<EmptyProjection>();
    builder.ProjectionEntity<BankAccountProjection>();
  }
}

public class SqlServerTestContextFactory : IDesignTimeDbContextFactory<SqlServerTestContext>
{
  public SqlServerTestContext CreateDbContext(string[] args)
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();

    return new SqlServerTestContext(new DbContextOptionsBuilder<SqlServerTestContext>()
      .UseSqlServer(configuration.GetConnectionString("RecordStore"))
      .Options);
  }
}