using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public static class QueryableExtensions
  {
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> source) => (IAsyncEnumerable<T>) source;

    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
      var list = new List<T>();
      await foreach (var element in source.ToAsyncEnumerable().WithCancellation(cancellationToken))
        list.Add(element);
      return list;
    }
  }
}