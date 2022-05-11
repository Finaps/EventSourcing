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
        if (configuration.GetConnectionString("RecordStore") is { } postgresConnectionString)
        {
            services.AddDbContext<RecordContext, ExampleContext>(options =>
            {
                options.UseNpgsql(postgresConnectionString);
            });
            services.AddScoped<IRecordStore, EntityFrameworkRecordStore>();
        }
        else
        {
            services.Configure<CosmosRecordStoreOptions>(configuration.GetSection("Cosmos"));
            services.AddSingleton<IRecordStore, CosmosRecordStore>();
        }
        
        return services.AddScoped<IAggregateService, AggregateService>();
    }
}