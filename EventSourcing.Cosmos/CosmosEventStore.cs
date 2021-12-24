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
using Microsoft.Extensions.Options;

namespace EventSourcing.Cosmos
{
  public class CosmosEventStore : CosmosEventStore<Event>, IEventStore
  {
    public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options) { }
  }

  /// <summary>
  /// Cosmos Event Store: Cosmos Connection for Querying and Storing <see cref="TBaseEvent"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
  public class CosmosEventStore<TBaseEvent> : CosmosClientBase<TBaseEvent>, IEventStore<TBaseEvent>
    where TBaseEvent : Event, new()
  {
    private readonly TransactionalBatchItemRequestOptions _batchItemRequestOptions = new()
    {
      EnableContentResponseOnWrite = false
    };

    private readonly Container _events;

    public CosmosEventStore(IOptions<CosmosEventStoreOptions> options) : base(options)
    {
      if (string.IsNullOrWhiteSpace(options.Value.EventsContainer))
        throw new ArgumentException("CosmosEventStoreOptions.EventsContainer should not be empty", nameof(options));

      _events = _database.GetContainer(options.Value.EventsContainer);
    }

    /// <summary>
    /// Events: Queryable and AsyncEnumerable Collection of <see cref="TBaseEvent"/>s
    /// </summary>
    /// <typeparam name="TBaseEvent"></typeparam>
    public IQueryable<TBaseEvent> Events =>
      new CosmosAsyncQueryable<TBaseEvent>(_events.GetItemLinqQueryable<TBaseEvent>());

    /// <summary>
    /// AddAsync: Store <see cref="TBaseEvent"/>s to the Cosmos Event Store
    /// </summary>
    /// <param name="events"><see cref="TBaseEvent"/>s to add</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <exception cref="ArgumentException">Thrown when trying to add <see cref="TBaseEvent"/>s with empty AggregateId</exception>
    /// <exception cref="ArgumentException">Thrown when trying to add <see cref="TBaseEvent"/>s with equal AggregateVersions</exception>
    /// <exception cref="EventStoreException">Thrown when conflicts occur when storing <see cref="TBaseEvent"/>s</exception>
    /// <exception cref="ConcurrencyException">Thrown when storing <see cref="TBaseEvent"/>s</exception> with existing partition key and version combination
    public async Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
    {
      if (events == null) throw new ArgumentNullException(nameof(events));
      if (events.Count == 0) return;

      await VerifyAsync(events);

      var batch = _events.CreateTransactionalBatch(new PartitionKey(events.First().AggregateId.ToString()));
      foreach (var @event in events) batch.CreateItem(@event, _batchItemRequestOptions);
      var response = await batch.ExecuteAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) Throw(response, events);
    }

    private async Task<bool> ExistsAsync(Guid aggregateId, uint version)
    {
      var result =
        await _events.ReadItemStreamAsync(version.ToString(), new PartitionKey(aggregateId.ToString()));
      return result.IsSuccessStatusCode;
    }

    private async Task VerifyAsync(IList<TBaseEvent> events)
    {
      var aggregateIds = events.Select(x => x.AggregateId).Distinct().ToList();

      if (aggregateIds.Count > 1)
        throw new ArgumentException("All Events should have the same AggregateId", nameof(events));

      if (aggregateIds.Single() == Guid.Empty)
        throw new ArgumentException(
          "AggregateId should be set, did you forget to Add Events to an Aggregate?", nameof(events));

      if (events.Select((e, index) => e.AggregateVersion - index).Distinct().Skip(1).Any())
        throw new InvalidOperationException("Event versions should be consecutive");

      if (events[0].AggregateVersion != 0 && !await ExistsAsync(events[0].AggregateId, events[0].AggregateVersion - 1))
        throw new InvalidOperationException(
          $"Attempted to add nonconsecutive Event '{events[0].Type}' with Version {events[0].AggregateVersion} for Aggregate '{events[0].AggregateType}' with Id '{events[0].AggregateId}': " +
          $"no Event with Version {events[0].AggregateVersion - 1} exists");
    }

    private static void Throw(TransactionalBatchResponse response, IEnumerable<TBaseEvent> events)
    {
      if (response.StatusCode != HttpStatusCode.Conflict)
        throw new EventStoreException(
          $"Encountered error while adding events: {(int)response.StatusCode} {response.StatusCode.ToString()}",
          CreateCosmosException(response));

      throw new ConcurrencyException(response.Zip(events)
        .Where(x => x.First.StatusCode == HttpStatusCode.Conflict)
        .Select(x => x.Second)
        .FirstOrDefault(), CreateCosmosException(response));
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