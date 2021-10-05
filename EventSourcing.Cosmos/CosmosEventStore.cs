using System;
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

    public IQueryable<TEvent> Events => _container.GetItemLinqQueryable<TEvent>();

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = new TAggregate { Id = aggregateId };

      var events = Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable()
        .WithCancellation(cancellationToken);

      await foreach (var @event in events)
        aggregate.Add(@event);

      return aggregate;
    }
    
    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date, 
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = new TAggregate { Id = aggregateId };

      var events = Events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable()
        .WithCancellation(cancellationToken);

      await foreach (var @event in events)
        aggregate.Add(@event);

      return aggregate;
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var version = await Events
        .Where(x => x.AggregateId == aggregate.Id)
        .OrderByDescending(x => x.AggregateVersion)
        .Select(x => x.AggregateVersion + 1)
        .FirstOrDefaultAsync(cancellationToken);

      var batch = _container.CreateTransactionalBatch(new PartitionKey(aggregate.Id.ToString()));
      foreach (var @event in aggregate.Events.Skip(version)) batch.CreateItem(@event, _batchItemRequestOptions);
      await batch.ExecuteAsync(cancellationToken);

      return aggregate;
    }
  }
}