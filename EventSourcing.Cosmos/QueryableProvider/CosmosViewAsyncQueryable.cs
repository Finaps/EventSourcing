using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosViewAsyncQueryable<TAggregate, TView> : CosmosAsyncQueryable<TView>
    where TView : View<TAggregate>, new() where TAggregate : Aggregate, new()
  {
    private readonly CosmosViewStore _store;

    public CosmosViewAsyncQueryable(CosmosViewStore store, IQueryable<TView> queryable) : base(queryable)
    {
      _store = store;
    }
    
    public override async IAsyncEnumerator<TView> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      var iterator = Queryable.ToFeedIterator();
      var aggregate = new TAggregate();

      while (iterator.HasMoreResults)
      {
        FeedResponse<TView> views;

        try
        {
          views = await iterator.ReadNextAsync(cancellationToken);
        }
        catch (CosmosException e)
        {
          throw new ViewStoreException($"Encountered error while querying views: {(int)e.StatusCode} {e.StatusCode.ToString()}", e);
        }

        if (views == null) continue;

        foreach (var view in views)
        {
          yield return view.Hash != aggregate.Hash
            ? await _store.RehydratePersistAndUpdateAsync<TAggregate, TView>(view.Id, cancellationToken)
            : view;
        }
      }
    }
  }
}