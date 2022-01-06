namespace EventSourcing.Core;

public abstract class Aggregate : Aggregate<Event> { }
  
/// <summary>
/// Abstract Base <see cref="Aggregate{TBaseEvent}"/>
/// </summary>
/// <typeparam name="TBaseEvent">Base <see cref="Event"/> Type</typeparam>
public abstract class Aggregate<TBaseEvent> where TBaseEvent : Event
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
  
  [JsonIgnore] public ImmutableArray<TBaseEvent> UncommittedEvents => _uncommittedEvents.ToImmutableArray();
  [JsonIgnore] private readonly List<TBaseEvent> _uncommittedEvents = new();

  /// <summary>
  /// Create new Aggregate
  /// </summary>
  public Aggregate()
  {
    Id = Guid.NewGuid();
    Type = GetType().Name;
  }
    
  /// <summary>
  /// Apply Event
  /// </summary>
  /// <param name="e"><see cref="Event"/> to apply</param>
  /// <typeparam name="TEvent"><see cref="Event"/> Type</typeparam>
  protected abstract void Apply<TEvent>(TEvent e) where TEvent : TBaseEvent;
    
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
  public TEvent Add<TEvent>(TEvent e) where TEvent : TBaseEvent
  {
    e = e with
    {
      AggregateId = Id,
      AggregateType = Type,
      AggregateVersion = Version
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
  public static async Task<TAggregate> RehydrateAsync<TAggregate>(Guid id, IAsyncEnumerable<TBaseEvent> events,
    CancellationToken cancellationToken = default) where TAggregate : Aggregate<TBaseEvent>, new()
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
  private void ValidateAndApply<TEvent>(TEvent e) where TEvent : TBaseEvent
  {
    if (e.EventId == Guid.Empty)
      throw new ArgumentException("Event.Id should not be empty", nameof(e));
      
    if (e.Type != e.GetType().Name)
      throw new ArgumentException($"Event.Type ({e.Type}) does not correspond with Class Type ({e.GetType().Name})", nameof(e));
      
    if (e.AggregateId != Id)
      throw new ArgumentException($"Event.AggregateId ({e.AggregateId}) does not correspond with Aggregate.Id ({Id})", nameof(e));

    if (e.AggregateType != GetType().Name)
      throw new ArgumentException($"Event.AggregateType ({e.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().Name})", nameof(e));
      
    if (e is SnapshotEvent && this is not ISnapshottable)
      throw new InvalidOperationException($"Cannot apply snapshot {e.GetType().Name} to non-snapshottable aggregate {GetType().Name}");
      
    if (e is not SnapshotEvent && e.AggregateVersion != Version)
      throw new InvalidOperationException($"Event.AggregateVersion ({e.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");

    Apply(e);
      
    if (e is SnapshotEvent)
      Version = e.AggregateVersion;
    else
      Version++;
  }
}