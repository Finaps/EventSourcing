using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.Core
{
  public class AggregateService : AggregateService<Event>, IAggregateService
  {
    public AggregateService(IEventStore events, IViewStore views) : base(events, views) { }
  }

  /// <summary>
  /// Aggregate Service: Rehydrating and Persisting <see cref="Aggregate"/>s from <see cref="Event"/>s
  /// </summary>
  /// <typeparam name="TBaseEvent"></typeparam>
  public class AggregateService<TBaseEvent> : IAggregateService<TBaseEvent> where TBaseEvent : Event
  {
    private readonly IEventStore<TBaseEvent> _events;
    private readonly IViewStore _views;
    
    public AggregateService(IEventStore<TBaseEvent> events, IViewStore views)
    {
      _events = events;
      _views = views;
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _events.Events
        .Where(x => x.AggregateId == aggregateId)
        .OrderBy(x => x.AggregateVersion)
        .ToAsyncEnumerable();

      return await Aggregate<TBaseEvent>.RehydrateAsync<TAggregate>(aggregateId, events, cancellationToken);
    }

    public async Task<TAggregate> RehydrateAsync<TAggregate>(Guid aggregateId, DateTimeOffset date,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
    {
      var events = _events.Events
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
      
      await _events.AddAsync(aggregate.UncommittedEvents.ToList(), cancellationToken);
      await _views.UpdateAsync(aggregate, cancellationToken);
      
      aggregate.ClearUncommittedEvents();
      return aggregate;
    }
  }
}