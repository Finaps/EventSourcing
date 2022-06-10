using EventSourcing.EF.SqlAggregate;
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
  public PostgresTestRecordContext CreateDbContext(string[] args) => 
    new (new DbContextOptionsBuilder<PostgresTestRecordContext>()
      .UseNpgsql(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()
        .GetConnectionString("RecordStore"))
      .UseAllCheckConstraints()
      .EnableSensitiveDataLogging()
      .Options);
}