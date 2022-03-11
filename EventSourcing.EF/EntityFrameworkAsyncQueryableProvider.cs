using System.Collections;
using System.Linq.Expressions;
using System.Text.Json;
using EventSourcing.Core;
using EventSourcing.EF;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.EntityFramework;

internal class EntityFrameworkAsyncQueryable<TResult> : IOrderedQueryable<TResult>, IAsyncEnumerable<TResult>
{
  private static RecordSerializer _serializer = new RecordSerializer(new JsonSerializerOptions
  {
    Converters = { new RecordConverter<Event>(), new RecordConverter<Snapshot>() }
  });
  
  private readonly IQueryable<TResult> _queryable;

  public EntityFrameworkAsyncQueryable(IQueryable<TResult> queryable)
  {
    _queryable = queryable;
    Provider = new EntityFrameworkAsyncQueryableProvider(queryable.Provider);
  }

  public Type ElementType => typeof(TResult);

  public Expression Expression => _queryable.Expression;

  public IQueryProvider Provider { get; }

  public IEnumerator<TResult> GetEnumerator() => _queryable.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();

  public async IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
  {
    await foreach (var item in EntityFrameworkQueryableExtensions.AsAsyncEnumerable(_queryable).WithCancellation(cancellationToken))
    {
      yield return item switch
      {
        Snapshot s => _serializer.Deserialize<TResult>(s as SnapshotEntity),
        Event e => _serializer.Deserialize<TResult>(e as EventEntity),
        _ => item
      };
    }
  }
}

internal class EntityFrameworkAsyncQueryableProvider : IQueryProvider
{
  private readonly IQueryProvider _provider;
  public EntityFrameworkAsyncQueryableProvider(IQueryProvider provider) =>
    _provider = provider;

  public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
    new EntityFrameworkAsyncQueryable<TElement>(_provider.CreateQuery<TElement>(expression));

  public IQueryable CreateQuery(Expression expression) => CreateQuery<object>(expression);
  public object? Execute(Expression expression) => _provider.Execute(expression);
  public TResult Execute<TResult>(Expression expression) => _provider.Execute<TResult>(expression);
}