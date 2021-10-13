using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core.Tests.InMemoryEventStore
{
    internal class InMemoryEventStore : InMemoryEventStore<Event>, IEventStore
    {
    }

    internal class InMemoryEventStore<TBaseEvent> : IEventStore<TBaseEvent> where TBaseEvent : Event, new()
    {
        private readonly ConcurrentDictionary<(Guid,int), TBaseEvent> _storedEvents = new();

        public IQueryable<TBaseEvent> Events => new MockAsyncQueryable<TBaseEvent>(_storedEvents.Values.AsQueryable());

        public Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
        {
            if (events == null || events.Count == 0)
                return Task.CompletedTask;
            
            var aggregateId = events.First().AggregateId;
            if (events.Any(e => e.AggregateId != aggregateId))
                throw new EventStoreException("Cannot add multiple events with different aggregate id's");
            
            if(events.Any(e => _storedEvents.ContainsKey((e.AggregateId, e.AggregateVersion))))
                throw new EventStoreException("", new ConflictException($"Conflict when persisting events to {nameof(InMemoryEventStore)}"));
            
            foreach (var e in events)
            {
                var added = _storedEvents.TryAdd((aggregateId, e.AggregateVersion), e);
                if(!added)
                    throw new EventStoreException("", new ConflictException($"Conflict when persisting events to {nameof(InMemoryEventStore)}"));
            }
            return Task.CompletedTask;
        }
    }
}

