
using Finaps.EventSourcing.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Finaps.EventSourcing.EF;

/// <summary>
/// Extension methods for EF ease of setup 
/// </summary>
public static class ServiceProviderExtensions
{
  /// <summary>
  /// Extension method of the IServiceCollection to provide
  /// Registration of IRecordStore and IAggregate.
  /// </summary>
  /// <param name="serviceProvider"></param>
  /// <typeparam name="T">Your custom DbContext which inherits from Record Context</typeparam>
  /// <returns></returns>
  public static IServiceCollection AddEventSourcing<T>(this IServiceCollection serviceProvider) where T : RecordContext
  {
    return serviceProvider
    .AddScoped<IRecordStore>(provider => new EntityFrameworkRecordStore(provider.GetService<T>() 
      ?? throw new InvalidOperationException($"Could not find service '{typeof(T)}'. " +
                                             "Did you add this service to the Service Provider?")))
    .AddScoped<IRecordStore, EntityFrameworkRecordStore>()
    .AddScoped<IAggregateService, AggregateService>();
  }
}