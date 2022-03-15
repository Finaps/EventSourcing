using System;
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
    
    var mockProjection = builder.ProjectionEntity<MockAggregateProjection>();
    mockProjection.OwnsOne(x => x.MockNestedRecord);
    mockProjection.OwnsMany(x => x.MockNestedRecordList);

    builder.ProjectionEntity<EmptyProjection>();
    builder.ProjectionEntity<BankAccountProjection>();
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