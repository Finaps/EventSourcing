using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
  public class CosmosEventStore : CosmosEventStore<Event>, IEventStore
  {
    public CosmosEventStore(IOptions<ComosEventStoreOptions> options) : base(options) { }
  }

  public class CosmosEventStore<TEvent> : IEventStore<TEvent> where TEvent : Event, new()
  {
    private readonly CosmosClientOptions _clientOptions = new()
    {
      Serializer = new CosmosEventSerializer(new JsonSerializerOptions
      {
        Converters = { new EventConverter() }
      })
    };

    private readonly TransactionalBatchItemRequestOptions _batchItemRequestOptions = new()
    {
      EnableContentResponseOnWrite = false
    };

    private readonly IOptions<ComosEventStoreOptions> _options;
    private readonly Database _database;
    private readonly Container _container;

    public CosmosEventStore(IOptions<ComosEventStoreOptions> options)
    {
      _options = options;
      _database = new CosmosClient(options.Value.ConnectionString, _clientOptions).GetDatabase(options.Value.Database);
      _container = _database.GetContainer(options.Value.Container);
    }

    public async Task CreateIfNotExistsAsync()
    {
      await _database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(_options.Value.Container, $"/{nameof(Event.AggregateId)}")
        {
          UniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys =
          {
            new UniqueKey { Paths = { $"/{nameof(Event.AggregateId)}", $"/{nameof(Event.AggregateVersion)}" }}
          }}
        });
    }
    
    public IAsyncEnumerable<T> Query<T>(Func<IQueryable<TEvent>, IQueryable<T>> func) => 
      func(_container.GetItemLinqQueryable<TEvent>()).ToAsyncEnumerable();

    public async Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
    {
      var partition = new PartitionKey(events.Select(x => x.AggregateId).Distinct().Single().ToString());
      var batch = _container.CreateTransactionalBatch(partition);
      foreach (var @event in events) batch.CreateItem(@event, _batchItemRequestOptions);
      await batch.ExecuteAsync(cancellationToken);
    }
  }
}