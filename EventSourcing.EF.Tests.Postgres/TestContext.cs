using System;
using EventSourcing.Core.Tests;
using EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EventSourcing.EF.Tests;

public class TestContext : RecordContext
{
  public TestContext(DbContextOptions<TestContext> options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder
      .Entity<EventEntity>()
      .Property(x => x.Json)
      .HasColumnType("jsonb");
    
    builder
      .Entity<SnapshotEntity>()
      .Property(x => x.Json)
      .HasColumnType("jsonb");

    var mockProjection = builder.ProjectionEntity<MockAggregateProjection>();
    mockProjection.OwnsOne(x => x.MockNestedRecord);
    mockProjection.OwnsMany(x => x.MockNestedRecordList);

    builder.ProjectionEntity<EmptyProjection>();
    builder.ProjectionEntity<BankAccountProjection>();
  }
}

public class TestContextFactory : IDesignTimeDbContextFactory<TestContext>
{
  public TestContext CreateDbContext(string[] args)
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();
    
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    return new TestContext(new DbContextOptionsBuilder<TestContext>()
      .UseNpgsql(configuration.GetConnectionString("RecordStore"))
      .Options);
  }
}