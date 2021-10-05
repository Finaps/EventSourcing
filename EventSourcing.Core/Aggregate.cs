using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace EventSourcing.Core
{
  public abstract class Aggregate
  {
    public Guid Id { get; init; }
    public int Version => _events.Count;
    
    [JsonIgnore] public ImmutableArray<Event> Events => _events.ToImmutableArray();
    private readonly List<Event> _events = new();

    public Aggregate()
    {
      Id = Guid.NewGuid();
    }

    public TEvent Add<TEvent>(object data) where TEvent : Event, new()
    {
      var e = new TEvent
      {
        Id = Guid.NewGuid(),
        Type = typeof(TEvent).Name,
        AggregateType = GetType().Name,
        AggregateId = Id,
        AggregateVersion = Version,
        Timestamp = DateTimeOffset.Now
      };
      
      Map(data, e);

      return Add(e);
    }
  
    public TEvent Add<TEvent>(TEvent e) where TEvent : Event
    {
      if (e.AggregateId != Id)
        throw new InvalidOperationException($"Event.AggregateId ({e.AggregateId}) does not correspond with Aggregate.Id ({Id})");

      if (e.AggregateType != GetType().Name)
        throw new InvalidOperationException($"Event.AggregateType ({e.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().Name})");

      if (e.AggregateVersion != Version)
        throw new InvalidOperationException($"Event.AggregateVersion ({e.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");
      
      _events.Add(e);
      Apply(e);

      return e;
    }

    protected virtual void Apply(Event e) => Map(e);
    
    protected void Map(Event e) => Map(e, this);

    private static void Map(object source, object target)
    {
      var sourceType = source.GetType();
      foreach (var property in target.GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => sourceType.GetProperty(property.Name) != null && property.Name != nameof(Id)))
        property.SetValue(target, sourceType.GetProperty(property.Name)?.GetValue(source));
    }
  }
}