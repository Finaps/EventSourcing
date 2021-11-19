using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Core
{
  /// <summary>
  /// Abstract Base <see cref="Aggregate{TBaseEvent}"/>
  /// </summary>
  /// <typeparam name="TBaseEvent">Base <see cref="Event"/> Type</typeparam>
  public abstract class Aggregate : ITyped
  {
    /// <summary>
    /// Unique Aggregate identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// The number of events applied to this aggregate.
    /// </summary>
    public uint Version { get; private set; }
    
    /// <summary>
    /// Aggregate type
    /// </summary>
    public string Type { get; init; }

    public string Hash { get; init; }

    public string id => Id.ToString();
    
    [JsonIgnore] public ImmutableArray<Event> UncommittedEvents => _uncommittedEvents.ToImmutableArray();
    [JsonIgnore] private readonly List<Event> _uncommittedEvents = new();

    /// <summary>
    /// Create new Aggregate
    /// </summary>
    public Aggregate()
    {
      Id = Guid.NewGuid();
      Type = GetType().FullName;
      Hash = GetHash(this);
    }
    
    /// <summary>
    /// Apply Event
    /// </summary>
    /// <param name="e"><see cref="Event"/> to apply</param>
    /// <typeparam name="TEvent"><see cref="Event"/> Type</typeparam>
    protected abstract void Apply<TEvent>(TEvent e) where TEvent : Event;
    
    /// <summary>
    /// Called after Applying all events
    /// <remarks>Can be used to apply time-dependent updates</remarks>
    /// </summary>
    protected virtual void Finish() { }
    
    /// <summary>
    /// Add Event to Aggregate
    /// </summary>
    /// <remarks>
    /// Will Apply <see cref="Event"/> and add it to UncommittedEvents.
    /// To Persist the aggregate, call <c>IEventService.PersistAsync()</c>.
    /// </remarks>
    /// <param name="e"><see cref="Event"/> to add</param>
    /// <typeparam name="TEvent"><see cref="Event"/> Type</typeparam>
    /// <returns>Added <see cref="Event"/></returns>
    /// <exception cref="ArgumentException">Thrown when an invalid <see cref="Event"/> is added.</exception>
    public TEvent Add<TEvent>(TEvent e) where TEvent : Event
    {
      e = e with
      {
        AggregateId = Id,
        AggregateType = Type,
        AggregateVersion = Version,
      };

      ValidateAndApply(e);
      _uncommittedEvents.Add(e);
      return e;
    }

    /// <summary>
    /// Rehydrate <see cref="Aggregate{TBaseEvent}"/> from <see cref="Event"/> stream.
    /// </summary>
    /// <param name="id">Unique Aggregate identifier</param>
    /// <param name="events"><see cref="Event"/> stream</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <typeparam name="TAggregate"><see cref="Aggregate{TBaseEvent}"/> Type</typeparam>
    /// <returns><see cref="Aggregate{TBaseEvent}"/> of type <c>TAggregate</c></returns>
    /// <exception cref="ArgumentException">Thrown when <c>id</c> or <c>events</c> are invalid</exception>
    public static async Task<TAggregate> RehydrateAsync<TAggregate>(Guid id, IAsyncEnumerable<Event> events,
      CancellationToken cancellationToken = default) where TAggregate : Aggregate, new()
    {
      if (id == Guid.Empty)
        throw new ArgumentException("Aggregate Id should not be empty", nameof(id));

      var aggregate = new TAggregate { Id = id };
      await foreach (var e in events.WithCancellation(cancellationToken))
        aggregate.ValidateAndApply(e);
      
      aggregate.Finish();
      
      // If no Events have been applied (i.e. no events could be found), return null
      // Otherwise, return Aggregate
      return aggregate.Version == 0 ? null : aggregate;
    }

    /// <summary>
    /// Clear Uncommitted Events
    /// </summary>
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Validate and Apply <see cref="Event"/> to <see cref="Aggregate{TBaseEvent}"/>
    /// </summary>
    /// <param name="e"><see cref="Event"/> to Validate and Apply</param>
    /// <typeparam name="TEvent"><see cref="Event"/> type</typeparam>
    /// <exception cref="ArgumentException">Thrown when <see cref="Event"/> is invalid for this <see cref="Aggregate{TBaseEvent}"/></exception>
    /// <exception cref="InvalidOperationException">Thrown when on a version mismatch between <see cref="Event"/> and <see cref="Aggregate{TBaseEvent}"/></exception>
    private void ValidateAndApply<TEvent>(TEvent e) where TEvent : Event
    {
      if (e.EventId == Guid.Empty)
        throw new ArgumentException("Event.Id should not be empty", nameof(e));
      
      if (e.Type != e.GetType().FullName)
        throw new ArgumentException($"Event.Type ({e.Type}) does not correspond with Class Type ({e.GetType().FullName})", nameof(e));
      
      if (e.AggregateId != Id)
        throw new ArgumentException($"Event.AggregateId ({e.AggregateId}) does not correspond with Aggregate.Id ({Id})", nameof(e));

      if (e.AggregateType != GetType().FullName)
        throw new ArgumentException($"Event.AggregateType ({e.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().FullName})", nameof(e));
      
      if (e.AggregateVersion != Version)
        throw new InvalidOperationException($"Event.AggregateVersion ({e.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");

      Apply(e);
      Version++;
    }

    private static string GetHash<TAggregate>(TAggregate aggregate) where TAggregate : Aggregate
    {
      var method = aggregate.GetType().GetMethod(nameof(Apply), BindingFlags.NonPublic | BindingFlags.Instance);
      var data = method?.GetMethodBody()?.GetILAsByteArray();
      if (data == null) throw new NullReferenceException("Cannot compute hash for Aggregate");

      using var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
      return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
    }
  }
}