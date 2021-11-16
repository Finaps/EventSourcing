using System.Linq;
using System.Linq.Expressions;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosAsyncQueryableProvider : IQueryProvider
  {
    private readonly IQueryProvider _provider;
    public CosmosAsyncQueryableProvider(IQueryProvider provider) =>
      _provider = provider;

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
      new CosmosEventAsyncQueryable<TElement>(_provider.CreateQuery<TElement>(expression));

    public IQueryable CreateQuery(Expression expression) =>
      CreateQuery<object>(expression);

    public object Execute(Expression expression) =>
      _provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression) =>
      _provider.Execute<TResult>(expression);
  }
}