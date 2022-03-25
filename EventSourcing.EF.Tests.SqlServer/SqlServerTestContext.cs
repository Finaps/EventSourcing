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

    builder.Entity<MockEvent>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
      entity.Property(x => x.MockFloatList).HasBinaryConversion();
      entity.Property(x => x.MockStringSet).HasStringConversion(";");
    });

    builder.Entity<MockSnapshot>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
      entity.Property(x => x.MockFloatList).HasBinaryConversion();
      entity.Property(x => x.MockStringSet).HasStringConversion(";");
    });
    
    builder.Entity<MockAggregateProjection>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
      entity.Property(x => x.MockFloatList).HasBinaryConversion();
      entity.Property(x => x.MockStringSet).HasStringConversion(";");
    });
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
      .UseAllCheckConstraints()
      .EnableSensitiveDataLogging()
      .Options);
  }
}