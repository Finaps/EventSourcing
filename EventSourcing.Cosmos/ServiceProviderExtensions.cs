
using Finaps.EventSourcing.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Finaps.EventSourcing.Cosmos;

/// <summary>
/// Extension methods for Cosmos ease of setup 
/// </summary>
public static class ServiceProviderExtensions
{
  /// <summary>
  /// Extension method of the IServiceCollection to provide
  /// Registration of IRecordStore and IAggregate as well as Cosmos client options.
  /// </summary>
  /// <param name="serviceProvider"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddEventSourcing(this IServiceCollection serviceProvider, IConfigurationSection configuration)
  {
    return serviceProvider
    .Configure<CosmosRecordStoreOptions>(configuration)
    .AddEventSourcing();
  }

  /// <summary>
  /// Extension method of the IServiceCollection to provide
  /// Registration of IRecordStore and IAggregate as well as Cosmos client options.
  /// </summary>
  /// <param name="serviceProvider"></param>
  /// <returns></returns>
  public static IServiceCollection AddEventSourcing(this IServiceCollection serviceProvider)
  {
    return serviceProvider
    .AddSingleton<IRecordStore, CosmosRecordStore>()
    .AddSingleton<IAggregateService, AggregateService>();
  }
}