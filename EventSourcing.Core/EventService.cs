using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core
{
  public class EventService : EventService<Event>, IEventService
  {
    public EventService(IEventStore store) : base(store) { }
  }

  public class EventService<TBaseEvent> : IEventService<TBaseEvent> where TBaseEvent : Event
  {
    private readonly IEventStore<TBaseEvent> _store;
    
    public EventService(IEventStore<TBaseEvent> store)
    {
      _store = store;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();
      
      return await RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();
      
      return await RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (aggregate.Id == Guid.Empty)
        throw new EventServiceException("Invalid aggregate id");
      
      await _store.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
    
    private async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, IAsyncEnumerable<TBaseEvent> events,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var aggregate = new TAggregate { Id = aggregateId };
      await foreach (var @event in events.WithCancellation(cancellationToken))
        aggregate.Add(@event, true);
      return aggregate;
    }
  }
}