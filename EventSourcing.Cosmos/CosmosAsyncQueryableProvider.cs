using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos
{
  internal class CosmosAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
  {
    private readonly IQueryable<TResult> _queryable;

    public CosmosAsyncQueryable(IQueryable<TResult> queryable)
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
        foreach (var item in await iterator.ReadNextAsync(cancellationToken))
          yield return item;
    }
  }

  internal class CosmosAsyncQueryableProvider : IQueryProvider
  {
    private readonly IQueryProvider _provider;
    public CosmosAsyncQueryableProvider(IQueryProvider provider) =>
      _provider = provider;

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
      new CosmosAsyncQueryable<TElement>(_provider.CreateQuery<TElement>(expression));

    public IQueryable CreateQuery(Expression expression) =>
      CreateQuery<object>(expression);

    public object Execute(Expression expression) =>
      _provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression) =>
      _provider.Execute<TResult>(expression);
  }
}