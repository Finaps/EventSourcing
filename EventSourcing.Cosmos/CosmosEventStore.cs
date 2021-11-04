using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
  public class CosmosEventStore : CosmosEventStore<Event>, IEventStore
  {
    public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options) { }
  }

  public class CosmosEventStore<TBaseEvent> : IEventStore<TBaseEvent> where TBaseEvent : Event, new()
  {
    private readonly CosmosClientOptions _clientOptions = new()
    {
      Serializer = new CosmosEventSerializer(new JsonSerializerOptions
      {
        Converters = { new EventConverter<TBaseEvent>() }
      })
    };

    private readonly TransactionalBatchItemRequestOptions _batchItemRequestOptions = new()
    {
      EnableContentResponseOnWrite = false
    };

    private readonly IOptions<CosmosEventStoreOptions> _options;
    private readonly Database _database;
    private readonly Container _container;

    public CosmosEventStore(IOptions<CosmosEventStoreOptions> options)
    {
      if (options?.Value == null)
        throw new ArgumentException("CosmosEventStoreOptions should not be null", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
        throw new ArgumentException("CosmosEventStoreOptions.ConnectionString should not be empty", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.Database))
        throw new ArgumentException("CosmosEventStoreOptions.Database should not be empty", nameof(options));
      
      if (string.IsNullOrWhiteSpace(options.Value.Container))
        throw new ArgumentException("CosmosEventStoreOptions.Container should not be empty", nameof(options));
      
      _options = options;
      _database = new CosmosClient(options.Value.ConnectionString, _clientOptions).GetDatabase(options.Value.Database);
      _container = _database.GetContainer(options.Value.Container);
    }

    public IQueryable<TBaseEvent> Events =>
      new CosmosAsyncQueryable<TBaseEvent>(_container.GetItemLinqQueryable<TBaseEvent>());

    public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
    {
      var partition = new PartitionKey(events.Select(x => x.AggregateId).First().ToString());
      
      var batch = _container.CreateTransactionalBatch(partition);
      foreach (var @event in events) batch.CreateItem(@event, _batchItemRequestOptions);
      var response = await batch.ExecuteAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) Throw(response, events);
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
    
    private static void Throw(TransactionalBatchResponse response, IEnumerable<TBaseEvent> events)
    {
      if (response.StatusCode == HttpStatusCode.Conflict)
        throw new DuplicateKeyException(response.Zip(events)
          .Where(x => x.First.StatusCode == HttpStatusCode.Conflict)
          .Select(x => x.Second)
          .Select(x => new DuplicateKeyException(
            $"Duplicate Id and/or Unique Constraint while adding {x.Type} with Id {x.Id}")));

      throw new EventStoreException(
      $"Encountered error while adding events: {(int)response.StatusCode} {response.StatusCode.ToString()}",
      CreateCosmosException(response));
    }

    private static CosmosException CreateCosmosException(TransactionalBatchResponse response)
    {
      var subStatusCode = (int) response
        .GetType()
        .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
        .GetValue(response)!;
      
      return new CosmosException(response.ErrorMessage, response.StatusCode, subStatusCode, response.ActivityId, response.RequestCharge);
    }
  }
}