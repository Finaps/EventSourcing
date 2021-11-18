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
    public CosmosViewStore(IOptions<CosmosStoreOptions> options) : base(options, new CosmosClientOptions
    {
      Serializer = new CosmosStoreSerializer(new JsonSerializerOptions
      {
        Converters = { new JsonTypedConverter<Aggregate>() }
      })
    }) { }
    
    public IQueryable<TView> Query<TView>() where TView : IView, new() =>
      new CosmosViewAsyncQueryable<TView>(Container.GetItemLinqQueryable<TView>().Where(x => x.Type == new TView().Type));
    
    public async Task<TView> Get<TView>(Guid id, CancellationToken cancellationToken = default) where TView : IView, new()
    {
      try
      {
        var response = await Container.ReadItemAsync<TView>(id.ToString(), 
          new PartitionKey(new TView().Type), cancellationToken: cancellationToken);
        return response.Resource;
      }
      catch (CosmosException e)
      {
        throw new ViewStoreException("Encountered error while reading aggregate", e);
      }
    }

    public async Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
      try
      {
        await Container.UpsertItemAsync(aggregate, new PartitionKey(aggregate.Type), cancellationToken: cancellationToken);
      }
      catch (CosmosException e)
      {
        throw new ViewStoreException("Encountered error while updating aggregate", e);
      }
    }
  }
}
