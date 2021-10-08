using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public abstract class Aggregate
  {
    public Guid Id { get; init; }
    public int Version { get; private set; }
    
    [JsonIgnore] public ImmutableArray<Event> UncommittedEvents => _uncommittedEvents.ToImmutableArray();
    private readonly List<Event> _uncommittedEvents = new();

    public Aggregate()
    {
      Id = Guid.NewGuid();
    }
    
    protected abstract void Apply(Event e);

    public void ClearUncommittedEvents()
    {
      _uncommittedEvents.Clear();
    }
    
    public TEvent Add<TEvent>(TEvent e, bool isFromHistory = false) where TEvent : Event
    {
      if (e.Id == Guid.Empty)
        throw new InvalidOperationException("Event should not have empty Id");

      if (e.AggregateType != GetType().Name)
        throw new InvalidOperationException($"Event.AggregateType ({e.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().Name})");
      
      if (e.AggregateId != Id)
        throw new InvalidOperationException($"Event.AggregateId ({e.AggregateId}) does not correspond with Aggregate.Id ({Id})");
      
      if (e.AggregateVersion != Version)
        throw new InvalidOperationException($"Event.AggregateVersion ({e.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");
      
      Apply(e);
      Version++;
      if(!isFromHistory)
        _uncommittedEvents.Add(e);

      return e;
    }
    
    protected Aggregate Map(Event e) => Mapper.Map(e, this, MapperExclude);

    private static readonly HashSet<string> MapperExclude = new(new[]
    {
      nameof(Id), nameof(Version), nameof(UncommittedEvents)
    });
  }
}