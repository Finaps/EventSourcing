using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core.Tests.Mocks
{
    public class InMemoryEventStore : InMemoryEventStore<Event>, IEventStore
    {
    }

    public class InMemoryEventStore<TEvent> : IEventStore<TEvent> where TEvent : Event, new()
    {
        private readonly ConcurrentDictionary<(Guid,int), TEvent> _storedEvents = new();
        public async IAsyncEnumerable<T> Query<T>(Func<IQueryable<TEvent>, IQueryable<T>> func)
        {
            var events = func(_storedEvents.Values.AsQueryable());
            foreach(var e in events)
            {
                await Task.CompletedTask;
                yield return e;
            }
        }

        public Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
        {
            foreach (var e in events)
            {
                var added = _storedEvents.TryAdd((e.AggregateId, e.AggregateVersion), (TEvent) e);
                if(!added)
                    throw new ConflictException($"Conflict when persisting events to {nameof(InMemoryEventStore)}");
            }
            return Task.CompletedTask;
        }
    }
}

