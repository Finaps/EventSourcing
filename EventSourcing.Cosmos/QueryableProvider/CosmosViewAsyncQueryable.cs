using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using EventSourcing.Core;
using Microsoft.Azure.Cosmos;

namespace EventSourcing.Cosmos.QueryableProvider
{
  internal class CosmosViewAsyncQueryable<TAggregate, TView> : CosmosAsyncQueryable<TView>
    where TView : View<TAggregate>, new() where TAggregate : Aggregate, new()
  {
    private readonly CosmosViewStore _store;
    private readonly TAggregate _aggregate;

    public CosmosViewAsyncQueryable(CosmosViewStore store, IQueryable<TView> queryable) : base(queryable)
    {
      _store = store;
      _aggregate = new TAggregate();
    }

    protected override async IAsyncEnumerable<TView> GetItemsAsync(FeedResponse<TView> feed, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      foreach (var view in feed)
        yield return view.Hash != _aggregate.Hash
          ? await _store.RehydratePersistAndUpdateAsync<TAggregate, TView>(view.Id, cancellationToken)
          : view;
    }
  }
}