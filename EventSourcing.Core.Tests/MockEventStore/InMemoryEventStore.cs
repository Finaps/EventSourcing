using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core.Tests.MockEventStore
{
  internal class InMemoryEventStore : InMemoryEventStore<Event>, IEventStore
  {
  }

  internal class InMemoryEventStore<TBaseEvent> : IEventStore<TBaseEvent> where TBaseEvent : Event, new()
  {
    private readonly ConcurrentDictionary<(Guid, uint), TBaseEvent> _storedEvents = new();
    
    public IQueryable<TBaseEvent> Events => new MockAsyncQueryable<TBaseEvent>(_storedEvents.Values.AsQueryable());

    public Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
    {
      if (events == null || events.Count == 0)
        return Task.CompletedTask;

      var aggregateId = events.First().AggregateId;
      
      if (events.Any(e => e.AggregateId != aggregateId))
        throw new ArgumentException("Cannot add multiple events with different aggregate id's", nameof(events));
      
      if (events.Select(x => x.AggregateVersion).Distinct().Count() != events.Count)
        throw new ArgumentException("Cannot add multiple events with equal versions", nameof(events));
      
      foreach (var e in events)
      {
        if (_storedEvents.ContainsKey((e.AggregateId, e.AggregateVersion)))
          throw new ConcurrencyException(e);
      }

      foreach (var e in events.Where(e => !_storedEvents.TryAdd((e.AggregateId, e.AggregateVersion), e)))
        throw new ConcurrencyException(e);

      return Task.CompletedTask;
    }
  }
}