using EventSourcing.Core;

namespace EventSourcing.Cosmos;

internal static class ContainerExtensions
{
  public static IQueryable<TBaseEvent> AsCosmosAsyncQueryable<TBaseEvent>(this Container container) 
    where TBaseEvent : Event, new() => new CosmosAsyncQueryable<TBaseEvent>(container.GetItemLinqQueryable<TBaseEvent>());
}