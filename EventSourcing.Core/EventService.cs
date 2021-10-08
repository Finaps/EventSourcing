using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public class EventService : EventService<Event>
  {
    public EventService(IEventStore store) : base(store) { }
  }

  public class EventService<TEvent> : IEventService where TEvent : Event
  {
    private readonly IEventStore<TEvent> _store;
    
    public EventService(IEventStore<TEvent> store)
    {
      _store = store;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      return await RehydrateAsync<TAggregate>(aggregateId, events => events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion
        ), cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      return await RehydrateAsync<TAggregate>(aggregateId, events => events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion
        ), cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      await _store.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
    
    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, Func<IQueryable<TEvent>, IQueryable<TEvent>> query,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      var aggregate = new TAggregate { Id = aggregateId };
      await foreach (var @event in _store.Query(query).WithCancellation(cancellationToken))
        aggregate.Add(@event, true);
      return aggregate;
    }
  }
}