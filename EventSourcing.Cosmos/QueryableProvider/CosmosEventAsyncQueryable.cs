using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosEventAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
  {
    private readonly IQueryable<TResult> _queryable;

    public CosmosEventAsyncQueryable(IQueryable<TResult> queryable)
    {
      _queryable = queryable;
      Provider = new CosmosAsyncQueryableProvider(queryable.Provider);
    }

    public Type ElementType => typeof(TResult);

    public Expression Expression => _queryable.Expression;

    public IQueryProvider Provider { get; }

    public IEnumerator<TResult> GetEnumerator() => _queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();

    public async IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      var iterator = _queryable.ToFeedIterator();
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