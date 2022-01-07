using System.Collections.Concurrent;
using EventSourcing.Core;
using EventSourcing.Core.Exceptions;

namespace EventSourcing.InMemory;

public class InMemoryEventTransaction<TBaseEvent> : IEventTransaction<TBaseEvent> where TBaseEvent : Event, new()
{
  private readonly ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> _events;
  private readonly ConcurrentDictionary<(Guid, ulong), TBaseEvent> _addedEvents = new();
  private readonly Guid _partitionId;
  
  public InMemoryEventTransaction(ConcurrentDictionary<(Guid, Guid, ulong), TBaseEvent> events, Guid partitionId)
  {
    _events = events;
    _partitionId = partitionId;
  }

  public Task AddAsync(IList<TBaseEvent> events, CancellationToken cancellationToken = default)
  {
    if (events == null) throw new ArgumentNullException(nameof(events));
    if (events.Count == 0) return Task.CompletedTask;
    
    var tenantIds = events.Select(x => x.PartitionId).Distinct().ToList();
    
    if (tenantIds.Count > 1)
      throw new ArgumentException("All Events should have the same TenantId", nameof(events));
    
    if (tenantIds.Single() != _partitionId)
      throw new ArgumentException("All Events in a Transaction should have the same TenantId: " +
                                  $"expected: '{_partitionId}', found '{tenantIds.Single()}'", nameof(events));
    
    var aggregateIds = events.Select(x => x.AggregateId).Distinct().ToList();

    if (aggregateIds.Count > 1)
      throw new ArgumentException("All Events should have the same AggregateId", nameof(events));

    if (aggregateIds.Single() == Guid.Empty)
      throw new ArgumentException(
        "AggregateId should be set, did you forget to Add Events to an Aggregate?", nameof(events));

    if (!Utils.IsConsecutive(events.Select(e => e.AggregateVersion).ToList()))
      throw new InvalidOperationException("Event versions should be consecutive");
    
    if (events.First().AggregateVersion != 0 && !_events.ContainsKey((_partitionId, events.First().AggregateId, events.First().AggregateVersion - 1)))
      throw new InvalidOperationException("Event versions should be consecutive");

    foreach (var e in events)
      if (_addedEvents.ContainsKey((e.AggregateId, e.AggregateVersion)))
        throw new ConcurrencyException(e);

    foreach (var e in events.Where(e => !_addedEvents.TryAdd((e.AggregateId, e.AggregateVersion), e)))
      throw new ConcurrencyException(e);
    
    return Task.CompletedTask;
  }

  public Task CommitAsync(CancellationToken cancellationToken = default)
  {
    foreach (var e in _addedEvents.Values)
      if (_events.ContainsKey((_partitionId, e.AggregateId, e.AggregateVersion)))
        throw new ConcurrencyException(e);

    foreach (var e in _addedEvents.Values.Where(e => !_events.TryAdd((_partitionId, e.AggregateId, e.AggregateVersion), e)))
      throw new ConcurrencyException(e);
    
    return Task.CompletedTask;
  }
}