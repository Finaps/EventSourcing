using Finaps.EventSourcing.EF;
using Finaps.EventSourcing.Example.Domain.Products;
using Microsoft.EntityFrameworkCore;

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