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
    
    public IQueryable<TView> Query<TAggregate, TView>()
      where TView : View<TAggregate>, new() where TAggregate : Aggregate, new() =>
      new CosmosAsyncQueryable<TView>(Container.GetItemLinqQueryable<TView>().Where(x => x.Type == typeof(TAggregate).FullName));
    
    public async Task<TView> Get<TAggregate, TView>(Guid id, CancellationToken cancellationToken = default)
      where TView : View<TAggregate>, new() where TAggregate : Aggregate, new() => 
      await Get<TView>(id, new PartitionKey(new TAggregate().Type), cancellationToken);

    public async Task UpsertAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default)
      where TAggregate : Aggregate, new()
    {
      try
      {
        await Container.UpsertItemAsync(aggregate, new PartitionKey(aggregate.Type), cancellationToken: cancellationToken);
      }
      catch (CosmosException e)
      {
        throw new ViewStoreException("Encountered error while upserting view", e);
      }
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
