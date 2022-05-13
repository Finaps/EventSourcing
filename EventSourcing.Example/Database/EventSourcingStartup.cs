using Finaps.EventSourcing.Cosmos;
using Finaps.EventSourcing.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finaps.EventSourcing.Example.Infrastructure;

public static class EventSourcingStartup
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, IConfigurationRoot configuration)
    {
        if (configuration.GetSection("Cosmos").Exists())
        {
            services.Configure<CosmosRecordStoreOptions>(configuration.GetSection("Cosmos"));
            services.AddSingleton<IRecordStore, CosmosRecordStore>();
        }
        else if (configuration.GetConnectionString("RecordStore") is not null)
        {
            services.AddDbContext<RecordContext, ExampleContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("RecordStore"));
            });
            services.AddScoped<IRecordStore, EntityFrameworkRecordStore>();
        }
        else throw new ArgumentException("No configuration found for event store", nameof(configuration));
        
        return services.AddScoped<IAggregateService, AggregateService>();
    }
}