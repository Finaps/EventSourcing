using System;
using EventSourcing.Core;
using EventSourcing.Core.Tests;
using EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EventSourcing.EF.Tests.Postgres;

public class PostgresTestContext : RecordContext
{
  public PostgresTestContext(DbContextOptions<PostgresTestContext> options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<MockEvent>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });

    builder.Entity<MockSnapshot>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });
    
    builder.Entity<MockAggregateProjection>(entity =>
    {
      entity.OwnsOne(x => x.MockNestedRecord);
      entity.OwnsMany(x => x.MockNestedRecordList);
    });
  }
}

public class TestContextFactory : IDesignTimeDbContextFactory<PostgresTestContext>
{
  public PostgresTestContext CreateDbContext(string[] args)
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();
    
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    return new PostgresTestContext(new DbContextOptionsBuilder<PostgresTestContext>()
      .UseNpgsql(configuration.GetConnectionString("RecordStore"))
      .Options);
  }
}