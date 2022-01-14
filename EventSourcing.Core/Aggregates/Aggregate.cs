namespace EventSourcing.Core;

/// <summary>
/// Abstract Base <see cref="Aggregate"/>
/// </summary>
public abstract class Aggregate
{
  /// <summary>
  /// Unique Partition identifier
  /// </summary>
  public Guid PartitionId { get; init; }
  
  /// <summary>
  /// Unique Aggregate identifier
  /// </summary>
  public Guid Id { get; init; }
    
  /// <summary>
  /// The number of events applied to this aggregate.
  /// </summary>
  public long Version { get; private set; }
    
  /// <summary>
  /// Aggregate type
  /// </summary>
  public string Type { get; init; }

  /// <summary>
  /// Uncommitted Events
  /// </summary>
  [JsonIgnore] public ImmutableArray<Event> UncommittedEvents => _uncommittedEvents.ToImmutableArray();
  [JsonIgnore] private readonly List<Event> _uncommittedEvents = new();

  /// <summary>
  /// Create new Aggregate
  /// </summary>
  protected Aggregate()
  {
    Id = Guid.NewGuid();
    Type = GetType().Name;
  }

  /// <summary>
  /// Apply Event
  /// </summary>
  /// <param name="e"><see cref="Event"/> to apply</param>
  protected abstract void Apply(Event e);

  /// <summary>
  /// Called after Applying all events
  /// <remarks>Can be used to apply time-dependent updates</remarks>
  /// </summary>
  protected virtual void Finish() { }
  
  /// <summary>
  /// Snapshot interval length
  /// </summary>
  public virtual long SnapshotInterval { get; }
  
  /// <summary>
  /// Create Snapshot
  /// </summary>
  /// <returns><see cref="SnapshotEvent"/></returns>
  protected virtual SnapshotEvent CreateSnapshot() => throw new NotImplementedException();
  
  /// <summary>
  /// Create Snapshot
  /// </summary>
  /// <returns></returns>
  public SnapshotEvent CreateLinkedSnapshot() => Link(CreateSnapshot());
    
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
    if (e is SnapshotEvent)
      throw new ArgumentException($"Cannot add SnapshotEvent {e}", nameof(e));
    
    e = Link(e);
    ValidateAndApply(e);
    _uncommittedEvents.Add(e);
    return e;
  }

  /// <summary>
  /// Rehydrate <see cref="Aggregate"/> from <see cref="Event"/> stream.
  /// </summary>
  /// <param name="partitionId">Unique Aggregate Partition identifier</param>
  /// <param name="id">Unique Aggregate identifier</param>
  /// <param name="events"><see cref="Event"/> stream</param>
  /// <param name="cancellationToken">Cancellation Token</param>
  /// <typeparam name="TAggregate"><see cref="Aggregate"/> Type</typeparam>
  /// <returns><see cref="Aggregate"/> of type <c>TAggregate</c></returns>
  /// <exception cref="ArgumentException">Thrown when <c>id</c> or <c>events</c> are invalid</exception>
  public static async Task<TAggregate> RehydrateAsync<TAggregate>(Guid partitionId, Guid id,
    IAsyncEnumerable<Event> events, CancellationToken cancellationToken = default)
    where TAggregate : Aggregate, new()
  {
    if (id == Guid.Empty)
      throw new ArgumentException("Aggregate Id should not be empty", nameof(id));

    var aggregate = new TAggregate { PartitionId = partitionId, Id = id };
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
  
  public bool SnapshotIntervalExceeded => SnapshotInterval != 0 &&
                                          (UncommittedEvents.First().AggregateVersion + 1) / SnapshotInterval !=
                                          (UncommittedEvents.Last().AggregateVersion + 1) / SnapshotInterval;

  private TEvent Link<TEvent>(TEvent e) where TEvent : Event => e with
  {
    PartitionId = PartitionId,
    AggregateId = Id,
    AggregateType = Type,
    AggregateVersion = Version
  };

  /// <summary>
  /// Validate and Apply <see cref="Event"/> to <see cref="Aggregate"/>
  /// </summary>
  /// <param name="e"><see cref="Event"/> to Validate and Apply</param>
  /// <exception cref="ArgumentException">Thrown when <see cref="Event"/> is invalid for this <see cref="Aggregate"/></exception>
  /// <exception cref="InvalidOperationException">Thrown when on a version mismatch between <see cref="Event"/> and <see cref="Aggregate"/></exception>
  private void ValidateAndApply(Event e)
  {
    if (e.EventId == Guid.Empty)
      throw new ArgumentException("Event.Id should not be empty", nameof(e));
      
    if (e.Type != e.GetType().Name)
      throw new ArgumentException($"Event.Type ({e.Type}) does not correspond with Class Type ({e.GetType().Name})", nameof(e));
      
    if (e.AggregateId != Id)
      throw new ArgumentException($"Event.AggregateId ({e.AggregateId}) does not correspond with Aggregate.Id ({Id})", nameof(e));

    if (e.AggregateType != GetType().Name)
      throw new ArgumentException($"Event.AggregateType ({e.AggregateType}) does not correspond with typeof(Aggregate) ({GetType().Name})", nameof(e));
    
    if (e.PartitionId != PartitionId)
      throw new ArgumentException($"Event.PartitionId ({e.PartitionId}) does not correspond with Aggregate.PartitionId ({PartitionId})", nameof(e));

    if (e is not SnapshotEvent && e.AggregateVersion != Version)
      throw new InvalidOperationException($"Event.AggregateVersion ({e.AggregateVersion}) does not correspond with Aggregate.Version ({Version})");

    Apply(e);

    Version = e is SnapshotEvent
      ? e.AggregateVersion
      : Version+1;
  }
}