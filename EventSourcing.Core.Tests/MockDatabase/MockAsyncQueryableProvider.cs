using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core.Tests.MockDatabase
{
  internal class MockAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
  {
    private readonly IQueryable<TResult> _queryable;

    public MockAsyncQueryable(IQueryable<TResult> queryable)
    {
      _queryable = queryable;
      Provider = new MockAsyncQueryableProvide(queryable.Provider);
    }

    public Type ElementType => typeof(TResult);

    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider { get; }


    public IEnumerator<TResult> GetEnumerator() => _queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();

    public async IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      foreach (var item in _queryable.ToList())
      {
        await Task.CompletedTask;
        yield return item;
      }
    }
  }

  internal class MockAsyncQueryableProvide : IQueryProvider
  {
    private readonly IQueryProvider _provider;

    public MockAsyncQueryableProvide(IQueryProvider provider) =>
      _provider = provider;

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
      new MockAsyncQueryable<TElement>(_provider.CreateQuery<TElement>(expression));

    public IQueryable CreateQuery(Expression expression) =>
      CreateQuery<object>(expression);

    public object Execute(Expression expression) =>
      _provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression) =>
      _provider.Execute<TResult>(expression);
  }
}