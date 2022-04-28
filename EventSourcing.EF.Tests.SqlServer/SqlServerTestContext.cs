using Finaps.EventSourcing.Core.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Finaps.EventSourcing.EF.Tests.SqlServer;

public class SqlServerTestContext : EntityFrameworkTestRecordContext
{
  public SqlServerTestContext(DbContextOptions<SqlServerTestContext> options) : base(options) {}
  
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<MockEvent>(entity =>
    {
      entity.Property(x => x.MockFloatList).HasBinaryConversion();
      entity.Property(x => x.MockStringSet).HasStringConversion(";");
    });

    builder.Entity<MockSnapshot>(entity =>
    {
      entity.Property(x => x.MockFloatList).HasBinaryConversion();
      entity.Property(x => x.MockStringSet).HasStringConversion(";");
    });
    
    builder.Entity<MockAggregateProjection>(entity =>
    {
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