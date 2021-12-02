using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core;
using EventSourcing.Core.Tests.MockEventStore;
using EventSourcing.Example.EventPublishing;

namespace EventSourcing.Example.EventService
{
    public class EventService : IEventStore
    {
        private static readonly IEventStore EventStore = new InMemoryEventStore();
        private readonly IEventPublisher _eventPublisher;

        public EventService(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }
        
        public IQueryable<Event> Events { get; } = EventStore.Events;
        public async Task AddAsync(IList<Event> events, CancellationToken cancellationToken = default)
        {
            await EventStore.AddAsync(events, cancellationToken);
            foreach (var @event in events)
            {
                _eventPublisher.Publish(@event);
            }
        }
    }
}