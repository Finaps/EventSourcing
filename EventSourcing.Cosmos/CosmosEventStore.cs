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
        Converters = { new EventConverter() }
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

    public IQueryable<TBaseEvent> Events =>
      new CosmosAsyncQueryable<TBaseEvent>(_container.GetItemLinqQueryable<TBaseEvent>());

    public async IAsyncEnumerable<T> Query<T>(Func<IQueryable<TBaseEvent>, IQueryable<T>> func)
    {
      var queryable = func(_container.GetItemLinqQueryable<TBaseEvent>());
      var iterator = queryable.ToFeedIterator();
      
      while (iterator.HasMoreResults)
        foreach (var item in await iterator.ReadNextAsync())
          yield return item;
    }
    
    public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
    {
      var partition = new PartitionKey(events.Select(x => x.AggregateId).First().ToString());
      
      var batch = _container.CreateTransactionalBatch(partition);
      foreach (var @event in events) batch.CreateItem(@event, _batchItemRequestOptions);
      var response = await batch.ExecuteAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) HandleExceptionAsync(response, events);
    }

    private void HandleExceptionAsync(TransactionalBatchResponse response, IList<TBaseEvent> events)
    {
      if (response.StatusCode == HttpStatusCode.Conflict)
        throw new CosmosEventStoreException(GetStatusCode(response), CreateConflictException(response, events));
      
      throw new CosmosEventStoreException(GetStatusCode(response));
    } 

    private ConflictException CreateConflictException(TransactionalBatchResponse response, IList<TBaseEvent> events)
    {
      var conflictingEventIndex = response.ToList()
        .FindIndex(x => x.StatusCode == HttpStatusCode.Conflict);

      if (conflictingEventIndex == -1)
        return new ConflictException("Conflict while persisting Event");

      var conflictingEvent = events[conflictingEventIndex];

      return new ConflictException(
        $"Conflict while persisting {conflictingEvent.Type} " +
        $"{new { conflictingEvent.Id, conflictingEvent.AggregateId, conflictingEvent.AggregateVersion }}.");
    }

    private static string GetStatusCode(TransactionalBatchResponse response)
    {
      var statusCode = response.StatusCode;
      var subStatusCode = (dynamic) response
        .GetType()
        .GetProperty("SubStatusCode", BindingFlags.NonPublic | BindingFlags.Instance)?
        .GetValue(response);
      return $"{(int) statusCode} {statusCode} ({subStatusCode})";
    }
  }
}