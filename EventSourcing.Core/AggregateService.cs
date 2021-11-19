using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  /// <summary>
  /// Aggregate Service: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
  public class AggregateService : IAggregateService
  {
    private readonly IEventStore<Event> _events;

    public AggregateService(IEventStore<Event> events)
    {
      _events = events;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var events = _events.Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();

      return await Aggregate.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var events = _events.Events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();
      
      return await Aggregate.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      if (aggregate.Id == Guid.Empty)
        throw new ArgumentException("Aggregate.Id cannot be empty", nameof(aggregate));
      
      await _events.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);

      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
  }
}