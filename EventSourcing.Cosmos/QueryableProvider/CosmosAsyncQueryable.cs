using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal abstract class CosmosAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
  {
    private readonly IQueryable<TResult> _queryable;

    protected CosmosAsyncQueryable(IQueryable<TResult> queryable)
    {
      _queryable = queryable;
      Provider = new CosmosAsyncQueryableProvider(queryable.Provider);
    }

    protected abstract IAsyncEnumerable<TResult> GetItemsAsync(FeedResponse<TResult> feed, CancellationToken cancellationToken = default);

    public async IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      var iterator = _queryable.ToFeedIterator();

      while (iterator.HasMoreResults)
      {
        FeedResponse<TResult> feed;

        try
        {
          feed = await iterator.ReadNextAsync(cancellationToken);
        }
        catch (CosmosException e)
        {
          throw new EventStoreException($"Encountered error while querying {typeof(TResult).Name}: {(int)e.StatusCode} {e.StatusCode.ToString()}", e);
        }

        if (feed == null) continue;

        await foreach (var result in GetItemsAsync(feed, cancellationToken))
          yield return result;
      }
    }
    
    public Type ElementType => typeof(TResult);

    public Expression Expression => _queryable.Expression;

    public IQueryProvider Provider { get; }

    public IEnumerator<TResult> GetEnumerator() => _queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();
  }
}