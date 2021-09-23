using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace EventSourcing.Core
{
  public abstract class Aggregate
  {
    public Guid Id { get; init; }
    public int Version => _events.Count;
    public ImmutableArray<Event> Events => _events.ToImmutableArray();
    private readonly List<Event> _events = new();

    public Aggregate()
    {
      Id = Guid.NewGuid();
    }
    
    public void Add(Event @event)
    {
      if (@event.AggregateId != Id)
        throw new InvalidOperationException($"Event.AggregateId ({@event.AggregateId}) does not correspond with Aggregate.Id ({Id})");

      if (@event.AggregateType != GetType().Name)
        throw new InvalidOperationException($"Event.AggregateType ({@event.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().Name})");

      if (@event.AggregateVersion != Version)
        throw new InvalidOperationException($"Event.AggregateVersion ({@event.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");
      
      _events.Add(@event);
      Apply(@event);
    }

    protected virtual void Apply(Event @event) => Map(@event);
    
    protected void Map(Event @event)
    {
      // For all properties declared directly in the inheriting class
      foreach (var property in GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        // If this property is listed on the Event
        if (@event.GetType().GetProperty(property.Name) != null)
          // Set property to the value in the event
          property.SetValue(this, @event.GetType().GetProperty(property.Name)?.GetValue(@event));
    }
  }
}