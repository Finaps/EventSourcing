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
  public class CosmosEventRepository : CosmosEventRepository<Event>
  {
    public CosmosEventRepository(IOptions<CosmosOptions> options) : base(options) { }
  }

  public class CosmosEventRepository<TEvent> : IEventRepository<TEvent> where TEvent : Event
  {
    private static readonly CosmosClientOptions ClientOptions = new()
    {
      Serializer = new CosmosEventSerializer(new JsonSerializerOptions
      {
        Converters = { new EventConverter() }
      })
    };

    private static readonly ItemRequestOptions ItemRequestOptions = new()
    {
      EnableContentResponseOnWrite = false
    };

    private static readonly TransactionalBatchItemRequestOptions BatchItemRequestOptions = new()
    {
      EnableContentResponseOnWrite = false
    };

    private readonly Container _container;
    
    public CosmosEventRepository(IOptions<CosmosOptions> options)
    {
      _container = new CosmosClient(options.Value.ConnectionString, ClientOptions)
        .GetDatabase(options.Value.Database)
        .GetContainer(options.Value.Container);
    }

    public IQueryable<TEvent> Events => _container.GetItemLinqQueryable<TEvent>();

    public async Task AppendAsync(Event @event, CancellationToken cancellationToken = default)
    {
      var key = new PartitionKey(@event.AggregateId.ToString());
      await _container.CreateItemAsync(@event, key, ItemRequestOptions, cancellationToken);
    }

    public async Task AppendAsync(Guid aggregateId, IEnumerable<Event> events, CancellationToken cancellationToken = default)
    {
      var batch = _container.CreateTransactionalBatch(new PartitionKey(aggregateId.ToString()));
      foreach (var @event in events) batch.CreateItem(@event, BatchItemRequestOptions);
      await batch.ExecuteAsync(cancellationToken);
    }
    
    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = new TAggregate
      {
        Id = aggregateId
      };

      var events = Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable()
        .WithCancellation(cancellationToken);

      await foreach (var @event in events)
        aggregate.Add(@event);

      return aggregate;
    }

    public async Task PersistAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var version = await Events
        .Where(x => x.AggregateId == aggregate.Id)
        .OrderByDescending(x => x.AggregateVersion)
        .Select(x => x.AggregateVersion + 1)
        .FirstOrDefaultAsync(cancellationToken);

      await AppendAsync(aggregate.Id, aggregate.Events.Skip(version), cancellationToken);
    }
  }
}