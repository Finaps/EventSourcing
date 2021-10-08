using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  public class EventService : EventService<Event>, IEventService
  {
    public EventService(IEventStore store) : base(store) { }
  }

  public class EventService<TBaseEvent> : IEventService<TBaseEvent> where TBaseEvent : Event
  {
    private readonly Dictionary<Guid, int> _versions = new ();
    private readonly IEventStore<TBaseEvent> _store;
    
    public EventService(IEventStore<TBaseEvent> store)
    {
      _store = store;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      return await RehydrateAsync<TAggregate>(aggregateId, events => events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion
        ), cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      return await RehydrateAsync<TAggregate>(aggregateId, events => events
        .Where(x => x.AggregateId == aggregateId && x.Timestamp <= date)
        .OrderBy(x => x.AggregateVersion
        ), cancellationToken);
    }

    public async Task<TAggregate> PersistAsync<TAggregate>(TAggregate aggregate,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      _versions.TryGetValue(aggregate.Id, out var version);
      await _store.AddAsync(aggregate.Events.Skip(version).ToList(), cancellationToken);
      _versions[aggregate.Id] = aggregate.Version;
      return aggregate;
    }
    
    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, Func<IQueryable<TBaseEvent>, IQueryable<TBaseEvent>> query,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var aggregate = new TAggregate { Id = aggregateId };
      await foreach (var @event in _store.Query(query).WithCancellation(cancellationToken))
        aggregate.Add(@event);
      _versions[aggregate.Id] = aggregate.Version;
      return aggregate;
    }
  }
}