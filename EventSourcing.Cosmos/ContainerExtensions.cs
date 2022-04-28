namespace Finaps.EventSourcing.Cosmos;

internal static class ContainerExtensions
{
  public static IQueryable<T> AsCosmosAsyncQueryable<T>(this Container container) =>
    new CosmosAsyncQueryable<T>(container.GetItemLinqQueryable<T>());
}