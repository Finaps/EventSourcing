using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosEventAsyncQueryable<TResult> : CosmosAsyncQueryable<TResult>
  {
    public CosmosEventAsyncQueryable(IQueryable<TResult> queryable) : base(queryable) { }

    public override async IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      var iterator = Queryable.ToFeedIterator();
      
      while (iterator.HasMoreResults)
      {
        FeedResponse<TResult> items;

        try
        {
          items = await iterator.ReadNextAsync(cancellationToken);
        }
        catch (CosmosException e)
        {
          throw new EventStoreException($"Encountered error while querying events: {(int)e.StatusCode} {e.StatusCode.ToString()}", e);
        }

        if (items == null) continue;
        
        foreach (var item in items)
          yield return item;
      }
    }
  }
}