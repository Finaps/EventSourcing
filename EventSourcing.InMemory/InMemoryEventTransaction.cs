using System.Collections.Concurrent;
using EventSourcing.Core;

namespace EventSourcing.InMemory;

public class InMemoryEventTransaction : IEventTransaction
{
  public Guid PartitionId { get; }

  private readonly ConcurrentDictionary<(Guid, Guid, long), Event> _events;
  private readonly List<Event> _addedEvents = new();
  private readonly Dictionary<Guid, long> _removedAggregateIds = new();

  public InMemoryEventTransaction(ConcurrentDictionary<(Guid, Guid, long), Event> events, Guid partitionId)
  {
    PartitionId = partitionId;
    _events = events;
  }

  public IEventTransaction Add(IList<Event> events)
  {
    RecordValidation.ValidateEventSequence(PartitionId, events);

    if (events.Count == 0) return this;

    lock (_addedEvents)
    {
      foreach (var e in events)
      {
        if (_addedEvents.Any(x => x.AggregateId == e.AggregateId && x.Index == e.Index))
          throw new EventStoreException(e);

        _addedEvents.Add(e);
      }
    }

    return this;
  }

  public IEventTransaction Delete(Guid aggregateId, long aggregateVersion)
  {
    lock (_removedAggregateIds)
    {
      _removedAggregateIds.Add(aggregateId, aggregateVersion);
    }

    return this;
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
        if (e.Index != 0 && !_events.ContainsKey((PartitionId, e.AggregateId, e.Index - 1)))
          throw new EventStoreException($"Couldn't find Event with version {e.Index-1} when adding Event with version {e.Index} for {e.AggregateType} with Id '{e.AggregateId}'");
        
        // When this event version is already present, throw
        if (!_events.TryAdd((PartitionId, e.AggregateId, e.Index), e))
          throw new EventStoreException(e);
      }

      foreach (var (aggregateId, version) in _removedAggregateIds)
      {
        // if there are more events than deletion expected, throw (events might have been added in the meantime)
        if (_events.ContainsKey((PartitionId, aggregateId, version)))
          throw new EventStoreException(_events.Values
            .SingleOrDefault(x =>
              x.PartitionId == PartitionId &&
              x.AggregateId == aggregateId &&
              x.Index == version)
          );
        
        // Remove all events with the specified aggregateId
        var toRemove = _events
          .Where(pair => pair.Value.AggregateId == aggregateId && pair.Value.Index < version)
          .Select(pair => pair.Key)
          .ToArray();

        if (toRemove.Length != version)
          throw new EventStoreException("Tried to remove more events than existing");

        foreach (var key in toRemove)
          _events.Remove(key, out _);
      }
    }

    return Task.CompletedTask;
  }
}