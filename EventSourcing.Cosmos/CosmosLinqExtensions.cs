using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos
{
  public static class CosmosLinqExtensions
  {
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
      var items = new List<T>();

      var iterator = queryable.ToFeedIterator();
      
      while (iterator.HasMoreResults)
        items.AddRange(await iterator.ReadNextAsync(cancellationToken));

      return items;
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable)
    {
      var iterator = queryable.ToFeedIterator();

      while (iterator.HasMoreResults)
        foreach (var item in await iterator.ReadNextAsync())
          yield return item;
    }

    public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
      var iterator = queryable
        .Take(1)
        .ToFeedIterator();
      
      return iterator.HasMoreResults ? (await iterator.ReadNextAsync(cancellationToken)).FirstOrDefault() : default;
    }
  }
}