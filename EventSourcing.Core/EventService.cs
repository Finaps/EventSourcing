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

  /// <summary>
  /// Event Service: Rehydrating and Persisting <see cref="Aggregate{TBaseEvent}"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
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

      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _store.Events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();
      
      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      if (aggregate.Id == Guid.Empty)
        throw new ArgumentException("Aggregate.Id cannot be empty", nameof(aggregate));
      
      await _store.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
  }
}