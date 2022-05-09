using Finaps.EventSourcing.EF;
using Finaps.EventSourcing.Example.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Finaps.EventSourcing.Example.Infrastructure;

public class ExampleContext : RecordContext
{
    public ExampleContext(DbContextOptions options) : base(options) {}
  
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Prevents an DateTimeOffset issue for Postgres
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Entity<ProductSnapshot>(entity =>
        {
            entity.OwnsMany(x => x.Reservations);
        });
    }
}

public class TestContextFactory : IDesignTimeDbContextFactory<ExampleContext>
{
    public ExampleContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile("appsettings.local.json", true)
            .AddEnvironmentVariables()
            .Build();
    
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        return new ExampleContext(new DbContextOptionsBuilder<ExampleContext>()
            .UseNpgsql(configuration.GetConnectionString("RecordStore"))
            .EnableSensitiveDataLogging()
            .Options);
    }
}