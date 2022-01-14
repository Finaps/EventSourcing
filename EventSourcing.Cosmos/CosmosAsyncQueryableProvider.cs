using System.Linq.Expressions;
using EventSourcing.Core;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos;

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