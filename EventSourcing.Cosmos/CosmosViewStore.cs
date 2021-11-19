using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using EventSourcing.Cosmos.QueryableProvider;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
  public class CosmosViewStore : CosmosStore, IViewStore
  {
    private readonly IAggregateService _service;
    
    public CosmosViewStore(IAggregateService service, IOptions<CosmosStoreOptions> options) : base(options, new CosmosClientOptions
    {
      Serializer = new CosmosStoreSerializer(new JsonSerializerOptions
      {
        Converters = { new JsonTypedConverter<Aggregate>() }
      })
    })
    {
      _service = service;
    }
    
    public IQueryable<TView> Query<TAggregate, TView>()
      where TView : View<TAggregate>, new() where TAggregate : Aggregate, new() =>
      new CosmosViewAsyncQueryable<TAggregate, TView>(
        this, Container.GetItemLinqQueryable<TView>().Where(x => x.Type == new TView().Type));
    
    public async Task<TView> Get<TAggregate, TView>(Guid id, CancellationToken cancellationToken = default)
      where TView : View<TAggregate>, new() where TAggregate : Aggregate, new()
    {
      var aggregate = new TAggregate();
      var result = await Get<TView>(id, new PartitionKey(aggregate.Type), cancellationToken);

      return result.Hash != aggregate.Hash
        ? await RehydratePersistAndUpdateAsync<TAggregate, TView>(id, cancellationToken)
        : result;
    }

    public async Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate
    {
      try
      {
        await Container.UpsertItemAsync(aggregate, new PartitionKey(aggregate.Type), cancellationToken: cancellationToken);
      }
      catch (CosmosException e)
      {
        throw new ViewStoreException("Encountered error while updating view", e);
      }
    }
    
    internal async Task<TView> RehydratePersistAndUpdateAsync<TAggregate, TView>(Guid id, CancellationToken cancellationToken)
      where TView : View<TAggregate>, new() where TAggregate : Aggregate, new()
    {
      var aggregate = await _service.RehydrateAndPersistAsync<TAggregate>(id, cancellationToken);

      await UpdateAsync(aggregate, cancellationToken);
        
      var view = await Get<TView>(id, new PartitionKey(aggregate.Type), cancellationToken);

      if (view.Hash != aggregate.Hash)
        throw new ViewStoreException("Found outdated view, but couldn't update");

      return view;
    }

    private async Task<T> Get<T>(Guid id, PartitionKey key, CancellationToken cancellationToken)
    {
      try
      {
        return await Container.ReadItemAsync<T>(id.ToString(), key, cancellationToken: cancellationToken);
      }
      catch (CosmosException e)
      {
        throw new ViewStoreException("Encountered error while reading view", e);
      }
    }
    

  }
}
