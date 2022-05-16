using Finaps.EventSourcing.Cosmos;
using Finaps.EventSourcing.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finaps.EventSourcing.Example.Infrastructure;

public static class EventSourcingStartup
{
    public static IServiceCollection StartupEventSourcing(this IServiceCollection services, IConfigurationRoot configuration)
    {
        if (configuration.GetValue<bool>("UseCosmos") && configuration.GetSection("Cosmos").Exists())
            // Call AddEventSourcing from the EventSourcing.Cosmos module
            services.AddEventSourcing(configuration.GetSection("Cosmos"));
        else if (configuration.GetConnectionString("RecordStore") is not null)
        {
            // Configure DbContext
            services.AddDbContext<RecordContext, ExampleContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("RecordStore"));
            });
            // Call AddEventSourcing from the EventSourcing.EF module
            services.AddEventSourcing<ExampleContext>();
        }
        else throw new ArgumentException("No configuration found for event store", nameof(configuration));

        return services;
    }
}