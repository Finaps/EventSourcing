using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Finaps.EventSourcing.EF.Tests.Postgres;

public class PostgresTestRecordContext : EntityFrameworkTestRecordContext
{
  public PostgresTestRecordContext(DbContextOptions<PostgresTestRecordContext> options) : base(options) {}
}

public class TestContextFactory : IDesignTimeDbContextFactory<PostgresTestRecordContext>
{
  public PostgresTestRecordContext CreateDbContext(string[] args)
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", false)
      .AddJsonFile("appsettings.local.json", true)
      .AddEnvironmentVariables()
      .Build();
    
    return new PostgresTestRecordContext(new DbContextOptionsBuilder<PostgresTestRecordContext>()
      .UseNpgsql(configuration.GetConnectionString("RecordStore"))
      .UseAllCheckConstraints()
      .EnableSensitiveDataLogging()
      .Options);
  }
}