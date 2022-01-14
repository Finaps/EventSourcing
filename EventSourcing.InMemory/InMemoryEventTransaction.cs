using System.Collections.Concurrent;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.InMemory;

public class InMemoryEventTransaction<TBaseEvent> : IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  public Guid PartitionId { get; }

  private readonly ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> _events;
  private readonly List<TBaseEvent> _addedEvents = new();
  private readonly Dictionary<Guid, ulong> _removedAggregateIds = new();

  public InMemoryEventTransaction(ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> events, Guid partitionId)
  {
    PartitionId = partitionId;
    _events = events;
  }

  public Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    EventValidation.Validate(PartitionId, events);

    if (events.Count == 0) return Task.CompletedTask;

    lock (_addedEvents)
    {
      foreach (var e in events)
      {
        if (_addedEvents.Any(x => x.AggregateId == e.AggregateId && x.AggregateVersion == e.AggregateVersion))
          throw new ConcurrencyException(e);

        _addedEvents.Add(e);
      }
    }
    
    return Task.CompletedTask;
  }

  public Task DeleteAsync(Guid aggregateId, CancellationToken cancellationToken = default)
  {
    lock (_removedAggregateIds)
    {
      _removedAggregateIds.Add(aggregateId, _events.Values
        .Where(x => x.AggregateId == aggregateId)
        .Max(x => x.AggregateVersion));
    }
    
    return Task.CompletedTask;
  }

  public Task CommitAsync(CancellationToken cancellationToken = default)
  {
    lock (_events)
    lock (_addedEvents)
    lock (_removedAggregateIds)
    {
      if (_addedEvents.Select(x => x.AggregateId).Intersect(_removedAggregateIds.Keys).Any())
        throw new InvalidOperationException("Cannot add and delete the same AggregateId in one transaction");
      
      foreach (var e in _addedEvents)
      {
        // When previous event version is not present, throw
        if (e.AggregateVersion != 0 && !_events.ContainsKey((PartitionId, e.AggregateId, e.AggregateVersion - 1)))
          throw new EventStoreException($"Couldn't find Event with version {e.AggregateVersion-1} when adding Event with version {e.AggregateVersion} for {e.AggregateType} with Id '{e.AggregateId}'");
        
        // When this event version is already present, throw
        if (!_events.TryAdd((PartitionId, e.AggregateId, e.AggregateVersion), e))
          throw new ConcurrencyException(e);
      }

      foreach (var (aggregateId, version) in _removedAggregateIds)
      {
        // if there are more events than deletion expected, throw (events might have been added in the meantime)
        if (_events.ContainsKey((PartitionId, aggregateId, version+1)))
          throw new ConcurrencyException(_events.Values
            .SingleOrDefault(x =>
              x.PartitionId == PartitionId &&
              x.AggregateId == aggregateId &&
              x.AggregateVersion == version)
          );
        
        // Remove all events with the specified aggregateId
        var toRemove = _events
          .Where(pair => pair.Value.AggregateId == aggregateId)
          .Select(pair => pair.Key);

        foreach (var key in toRemove)
          _events.Remove(key, out _);
      }
    }

    return Task.CompletedTask;
  }
}