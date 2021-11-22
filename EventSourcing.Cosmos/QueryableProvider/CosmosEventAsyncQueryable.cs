using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Azure.Cosmos;
#pragma warning disable CS1998

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosEventAsyncQueryable<TResult> : CosmosAsyncQueryable<TResult>
  {
    public CosmosEventAsyncQueryable(IQueryable<TResult> queryable) : base(queryable) { }

    protected override async IAsyncEnumerable<TResult> GetItemsAsync(FeedResponse<TResult> feed, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      foreach (var result in feed)
        yield return result;
    }
  }
}