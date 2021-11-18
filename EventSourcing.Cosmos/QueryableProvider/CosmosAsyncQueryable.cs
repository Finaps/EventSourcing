using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal abstract class CosmosAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
  {
    protected readonly IQueryable<TResult> Queryable;

    protected CosmosAsyncQueryable(IQueryable<TResult> queryable)
    {
      Queryable = queryable;
      Provider = new CosmosAsyncQueryableProvider(queryable.Provider);
    }

    public Type ElementType => typeof(TResult);

    public Expression Expression => Queryable.Expression;

    public IQueryProvider Provider { get; }

    public IEnumerator<TResult> GetEnumerator() => Queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Queryable.GetEnumerator();
    
    public abstract IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default);
  }
}