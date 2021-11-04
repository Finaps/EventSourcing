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
    private readonly ConcurrentDictionary<(Guid, int), TBaseEvent> _storedEvents = new();
    
    public IQueryable<TBaseEvent> Events => new MockAsyncQueryable<TBaseEvent>(_storedEvents.Values.AsQueryable());

    public Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
    {
      if (events == null || events.Count == 0)
        return Task.CompletedTask;

      var aggregateId = events.First().AggregateId;
      
      if (events.Any(e => e.AggregateId != aggregateId))
        throw new EventStoreException("Cannot add multiple events with different aggregate id's");

      var conflicts = new List<DuplicateKeyException>(events.Count);

      foreach (var e in events)
      {
        if (_storedEvents.Values.Select(x => x.Id).Contains(e.Id))
          conflicts.Add(DuplicateKeyException.CreateDuplicateIdException(e));
        
        if (_storedEvents.ContainsKey((e.AggregateId, e.AggregateVersion)))
          conflicts.Add(DuplicateKeyException.CreateDuplicateVersionException(e));
      }
      
      switch (conflicts.Count)
      {
        case 1:
          throw new DuplicateKeyException(conflicts.Single().Message);
        case > 1:
          throw new DuplicateKeyException(conflicts);
      }

      foreach (var e in events.Where(e => !_storedEvents.TryAdd((e.AggregateId, e.AggregateVersion), e)))
        throw DuplicateKeyException.CreateDuplicateVersionException(e);

      return Task.CompletedTask;
    }
  }
}